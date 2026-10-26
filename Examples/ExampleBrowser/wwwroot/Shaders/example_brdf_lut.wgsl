// Chapter06/02_BRDF_LUT. Based on the cookbook's main.comp and
// Sascha Willems' Vulkan-glTF-PBR genbrdflut shader.
@id(0) override NUM_SAMPLES: u32 = 1024u;
const PI = 3.1415926536;

struct Parameters {
    width: u32,
    height: u32,
    padding: vec2<u32>,
};

@group(0) @binding(0) var<uniform> parameters: Parameters;
// Two packed half-float pairs per texel: RG and BA. No shader-f16 feature required.
@group(0) @binding(1) var<storage, read_write> output: array<vec2<u32>>;

fn hammersley2d(i: u32) -> vec2<f32> {
    return vec2<f32>(f32(i) / f32(NUM_SAMPLES), f32(reverseBits(i)) * 2.3283064365386963e-10);
}

fn random(co: vec2<f32>) -> f32 {
    let dt = dot(co, vec2<f32>(12.9898, 78.233));
    // GLSL mod(x, y) = x - y * floor(x / y).
    let sn = dt - 3.14 * floor(dt / 3.14);
    return fract(sin(sn) * 43758.5453);
}

// The normal is always (0, 0, 1), so the cookbook's tangent frame is
// tangentX = (0, -1, 0), tangentY = (1, 0, 0).
fn importanceSampleGGX(xi: vec2<f32>, roughness: f32) -> vec3<f32> {
    let alpha = roughness * roughness;
    let phi = 2.0 * PI * xi.x + random(vec2<f32>(0.0, 1.0)) * 0.1;
    let cosTheta = sqrt((1.0 - xi.y) / (1.0 + (alpha * alpha - 1.0) * xi.y));
    let sinTheta = sqrt(max(1.0 - cosTheta * cosTheta, 0.0));
    return normalize(vec3<f32>(sinTheta * sin(phi), -sinTheta * cos(phi), cosTheta));
}

fn geometrySchlicksmithGGX(dotNL: f32, dotNV: f32, roughness: f32) -> f32 {
    let k = roughness * roughness / 2.0;
    let gl = dotNL / (dotNL * (1.0 - k) + k);
    let gv = dotNV / (dotNV * (1.0 - k) + k);
    return gl * gv;
}

fn visibilityAshikhmin(dotNL: f32, dotNV: f32) -> f32 {
    return clamp(1.0 / (4.0 * (dotNL + dotNV - dotNL * dotNV)), 0.0, 1.0);
}

fn distributionCharlie(roughness: f32, dotNH: f32) -> f32 {
    let invR = 1.0 / max(roughness, 0.000001);
    let sin2h = max(1.0 - dotNH * dotNH, 0.0);
    return (2.0 + invR) * pow(sin2h, invR * 0.5) / (2.0 * PI);
}

fn importanceSampleCharlie(xi: vec2<f32>, roughness: f32) -> vec3<f32> {
    let alpha = roughness * roughness;
    let phi = 2.0 * PI * xi.x;
    let sinTheta = pow(xi.y, alpha / (2.0 * alpha + 1.0));
    let cosTheta = sqrt(max(1.0 - sinTheta * sinTheta, 0.0));
    return normalize(vec3<f32>(sinTheta * sin(phi), -sinTheta * cos(phi), cosTheta));
}

fn integrateBRDF(noV: f32, roughness: f32) -> vec3<f32> {
    let v = vec3<f32>(sqrt(max(1.0 - noV * noV, 0.0)), 0.0, noV);
    var lut = vec3<f32>(0.0);
    for (var i = 0u; i < NUM_SAMPLES; i++) {
        let h = importanceSampleGGX(hammersley2d(i), roughness);
        let l = 2.0 * dot(v, h) * h - v;
        let dotNL = max(l.z, 0.0);
        let dotVH = max(dot(v, h), 0.0);
        let dotNH = max(h.z, 0.0);
        if (dotNL > 0.0) {
            let g = geometrySchlicksmithGGX(dotNL, noV, roughness);
            let gVis = (g * dotVH) / (dotNH * noV);
            let fc = pow(1.0 - dotVH, 5.0);
            lut.x += (1.0 - fc) * gVis;
            lut.y += fc * gVis;
        }
    }
    for (var i = 0u; i < NUM_SAMPLES; i++) {
        let h = importanceSampleCharlie(hammersley2d(i), roughness);
        let l = 2.0 * dot(v, h) * h - v;
        let dotNL = max(l.z, 0.0);
        let dotVH = max(dot(v, h), 0.0);
        let dotNH = max(h.z, 0.0);
        if (dotNL > 0.0) {
            lut.z += visibilityAshikhmin(dotNL, noV) * distributionCharlie(roughness, dotNH) * dotNL * dotVH;
        }
    }
    return lut / f32(NUM_SAMPLES);
}

@compute @workgroup_size(16, 16, 1)
fn cs_main(@builtin(global_invocation_id) id: vec3<u32>) {
    if (id.x >= parameters.width || id.y >= parameters.height) {
        return;
    }
    let uv = (vec2<f32>(id.xy) + vec2<f32>(0.5)) / vec2<f32>(f32(parameters.width), f32(parameters.height));
    let brdf = integrateBRDF(uv.x, 1.0 - uv.y);
    output[id.y * parameters.width + id.x] = vec2<u32>(pack2x16float(brdf.xy), pack2x16float(vec2<f32>(brdf.z, 1.0)));
}
