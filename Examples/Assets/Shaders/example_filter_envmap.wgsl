// Chapter06/03_FilterEnvmap, based on the cookbook's main.frag and
// Khronos glTF-Sample-Viewer/source/shaders/ibl_filtering.frag.
const PI = 3.141592653589793;
const LAMBERTIAN = 0u;
const GGX = 1u;
const CHARLIE = 2u;

struct Parameters {
    faceSize: u32,
    sourceWidth: u32,
    sourceHeight: u32,
    sampleCount: u32,
    roughness: f32,
    distribution: u32,
    outputOffset: u32,
    padding: u32,
};

@group(0) @binding(0) var<uniform> parameters: Parameters;
@group(0) @binding(1) var environment: texture_cube<f32>;
@group(0) @binding(2) var environmentSampler: sampler;
@group(0) @binding(3) var<storage, read_write> output: array<vec4<f32>>;

// WebGPU texture rows start at the top. v therefore increases down the face.
// Standard KTX/WebGPU face order: +X, -X, +Y, -Y, +Z, -Z.
fn faceDirection(face: u32, uv: vec2<f32>) -> vec3<f32> {
    switch face {
        case 0u: { return vec3<f32>(1.0, -uv.y, -uv.x); }
        case 1u: { return vec3<f32>(-1.0, -uv.y, uv.x); }
        case 2u: { return vec3<f32>(uv.x, 1.0, uv.y); }
        case 3u: { return vec3<f32>(uv.x, -1.0, -uv.y); }
        case 4u: { return vec3<f32>(uv.x, -uv.y, 1.0); }
        default: { return vec3<f32>(-uv.x, -uv.y, -1.0); }
    }
}

fn hammersley2d(i: u32) -> vec2<f32> {
    return vec2<f32>(f32(i) / f32(parameters.sampleCount), f32(reverseBits(i)) * 2.3283064365386963e-10);
}

struct DistributionSample {
    pdf: f32,
    cosTheta: f32,
    sinTheta: f32,
    phi: f32,
};

fn distributionGGX(nDotH: f32, alpha: f32) -> f32 {
    let a = nDotH * alpha;
    let k = alpha / max(1.0 - nDotH * nDotH + a * a, 1e-20);
    return k * k / PI;
}

fn distributionCharlie(roughness: f32, nDotH: f32) -> f32 {
    let invR = 1.0 / max(roughness, 0.000001);
    return (2.0 + invR) * pow(max(1.0 - nDotH * nDotH, 0.0), invR * 0.5) / (2.0 * PI);
}

fn sampleDistribution(xi: vec2<f32>) -> DistributionSample {
    var result: DistributionSample;
    result.phi = 2.0 * PI * xi.x;
    let alpha = parameters.roughness * parameters.roughness;
    switch parameters.distribution {
        case GGX: {
            result.cosTheta = clamp(sqrt((1.0 - xi.y) / (1.0 + (alpha * alpha - 1.0) * xi.y)), 0.0, 1.0);
            result.sinTheta = sqrt(max(1.0 - result.cosTheta * result.cosTheta, 0.0));
            result.pdf = distributionGGX(result.cosTheta, alpha) / 4.0;
        }
        case CHARLIE: {
            result.sinTheta = pow(xi.y, alpha / (2.0 * alpha + 1.0));
            result.cosTheta = sqrt(max(1.0 - result.sinTheta * result.sinTheta, 0.0));
            result.pdf = distributionCharlie(alpha, result.cosTheta) / 4.0;
        }
        default: {
            result.cosTheta = sqrt(1.0 - xi.y);
            result.sinTheta = sqrt(xi.y);
            result.pdf = result.cosTheta / PI;
        }
    }
    return result;
}

fn generateTBN(normal: vec3<f32>) -> mat3x3<f32> {
    var bitangent = vec3<f32>(0.0, 1.0, 0.0);
    if (1.0 - abs(normal.y) <= 0.0000001) {
        bitangent = vec3<f32>(0.0, 0.0, select(-1.0, 1.0, normal.y > 0.0));
    }
    let tangent = normalize(cross(bitangent, normal));
    return mat3x3<f32>(tangent, cross(normal, tangent), normal);
}

fn computeLod(pdf: f32) -> f32 {
    return 0.5 * log2(6.0 * f32(parameters.sourceWidth) * f32(parameters.sourceHeight)
        / (f32(parameters.sampleCount) * max(pdf, 1e-20)));
}

fn filterColor(normal: vec3<f32>) -> vec3<f32> {
    // At roughness zero GGX is a delta distribution. Avoid its singular PDF and
    // repeated identical texture fetches. Charlie's reflected directions have NdotL <= 0.
    if (parameters.roughness == 0.0 && parameters.distribution != LAMBERTIAN) {
        if (parameters.distribution == GGX) {
            return textureSampleLevel(environment, environmentSampler, normal, 0.0).rgb;
        }
        return vec3<f32>(0.0);
    }

    var color = vec3<f32>(0.0);
    var weights = 0.0;
    let tbn = generateTBN(normal);
    for (var i = 0u; i < parameters.sampleCount; i++) {
        let sample = sampleDistribution(hammersley2d(i));
        let localDirection = normalize(vec3<f32>(sample.sinTheta * cos(sample.phi),
            sample.sinTheta * sin(sample.phi), sample.cosTheta));
        let halfVector = tbn * localDirection;
        let lod = computeLod(sample.pdf);
        if (parameters.distribution == LAMBERTIAN) {
            color += textureSampleLevel(environment, environmentSampler, halfVector, lod).rgb;
        } else {
            let light = normalize(reflect(-normal, halfVector));
            let nDotL = dot(normal, light);
            if (nDotL > 0.0) {
                color += textureSampleLevel(environment, environmentSampler, light, lod).rgb * nDotL;
                weights += nDotL;
            }
        }
    }
    return color / select(f32(parameters.sampleCount), weights, weights > 0.0);
}

@compute @workgroup_size(8, 8, 1)
fn cs_main(@builtin(global_invocation_id) id: vec3<u32>) {
    if (id.x >= parameters.faceSize || id.y >= parameters.faceSize || id.z >= 6u) {
        return;
    }
    let uv = (vec2<f32>(id.xy) + vec2<f32>(0.5)) / f32(parameters.faceSize) * 2.0 - 1.0;
    let normal = normalize(faceDirection(id.z, uv));
    let index = parameters.outputOffset + id.z * parameters.faceSize * parameters.faceSize
        + id.y * parameters.faceSize + id.x;
    output[index] = vec4<f32>(filterColor(normal), 1.0);
}
