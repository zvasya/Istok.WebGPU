// Chapter06/04_MetallicRoughness: GGX + Burley direct light and split-sum IBL.
const PI = 3.141592653589793;

struct PerFrameData {
    model: mat4x4<f32>,
    normalMatrix: mat4x4<f32>,
    viewProjection: mat4x4<f32>,
    cameraPosition: vec4<f32>,
};
struct Material {
    baseColor: vec4<f32>,
    metallicRoughnessNormalOcclusion: vec4<f32>,
    emissiveAlphaCutoff: vec4<f32>,
    // base color, metallic/roughness, normal, occlusion UV sets
    textureCoordinates: vec4<u32>,
    // emissive UV, alpha mode, sRGB render target, unused
    flags: vec4<u32>,
};
@group(0) @binding(0) var<uniform> frame: PerFrameData;
@group(0) @binding(1) var<uniform> material: Material;

@group(1) @binding(0) var baseColorTexture: texture_2d<f32>;
@group(1) @binding(1) var metallicRoughnessTexture: texture_2d<f32>;
@group(1) @binding(2) var normalTexture: texture_2d<f32>;
@group(1) @binding(3) var occlusionTexture: texture_2d<f32>;
@group(1) @binding(4) var emissiveTexture: texture_2d<f32>;
@group(1) @binding(5) var baseColorSampler: sampler;
@group(1) @binding(6) var metallicRoughnessSampler: sampler;
@group(1) @binding(7) var normalSampler: sampler;
@group(1) @binding(8) var occlusionSampler: sampler;
@group(1) @binding(9) var emissiveSampler: sampler;

@group(2) @binding(0) var prefilteredEnvironment: texture_cube<f32>;
@group(2) @binding(1) var irradianceEnvironment: texture_cube<f32>;
@group(2) @binding(2) var brdfLut: texture_2d<f32>;
@group(2) @binding(3) var environmentSampler: sampler;

struct VertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) uv0uv1: vec4<f32>,
    @location(1) normal: vec3<f32>,
    @location(2) worldPosition: vec3<f32>,
    @location(3) color: vec4<f32>,
};

@vertex
fn vs_main(@location(0) position: vec3<f32>, @location(1) normal: vec3<f32>,
    @location(2) color: vec4<f32>, @location(3) uv0: vec2<f32>, @location(4) uv1: vec2<f32>) -> VertexOutput {
    var output: VertexOutput;
    let world = frame.model * vec4<f32>(position, 1.0);
    output.position = frame.viewProjection * world;
    output.worldPosition = world.xyz;
    output.normal = (frame.normalMatrix * vec4<f32>(normal, 0.0)).xyz;
    output.uv0uv1 = vec4<f32>(uv0, uv1);
    output.color = color;
    return output;
}

fn uv(input: VertexOutput, index: u32) -> vec2<f32> {
    return select(input.uv0uv1.xy, input.uv0uv1.zw, index == 1u);
}

fn perturbNormal(normal: vec3<f32>, position: vec3<f32>, coordinate: vec2<f32>, sample: vec3<f32>) -> vec3<f32> {
    let dp1 = dpdx(position);
    let dp2 = dpdy(position);
    let duv1 = dpdx(coordinate);
    let duv2 = dpdy(coordinate);
    let dp2perp = cross(dp2, normal);
    let dp1perp = cross(normal, dp1);
    var tangent = dp2perp * duv1.x + dp1perp * duv2.x;
    let bitangent = dp2perp * duv1.y + dp1perp * duv2.y;
    let inverseScale = inverseSqrt(max(max(dot(tangent, tangent), dot(bitangent, bitangent)), 1e-20));
    // Explicit handedness supports mirrored glTF UV islands.
    tangent *= select(1.0, -1.0, dot(cross(normal, tangent), bitangent) < 0.0);
    var map = sample * 2.0 - 1.0;
    map = vec3<f32>(map.xy * material.metallicRoughnessNormalOcclusion.z, map.z);
    return normalize(mat3x3<f32>(tangent * inverseScale, bitangent * inverseScale, normal) * normalize(map));
}

fn schlick(f0: vec3<f32>, f90: vec3<f32>, cosine: f32) -> vec3<f32> {
    return f0 + (f90 - f0) * pow(clamp(1.0 - cosine, 0.0, 1.0), 5.0);
}

fn directLight(n: vec3<f32>, v: vec3<f32>, worldPosition: vec3<f32>,
    diffuseColor: vec3<f32>, f0: vec3<f32>, roughness: f32, nDotV: f32) -> vec3<f32> {
    let light = normalize(vec3<f32>(0.0, 0.0, -5.0) - worldPosition);
    let halfVector = normalize(light + v);
    let nDotL = clamp(dot(n, light), 0.001, 1.0);
    let nDotH = clamp(dot(n, halfVector), 0.0, 1.0);
    let lDotH = clamp(dot(light, halfVector), 0.0, 1.0);
    let vDotH = clamp(dot(v, halfVector), 0.0, 1.0);
    let alpha = roughness * roughness;
    let alpha2 = alpha * alpha;
    let attenuationL = 2.0 * nDotL / (nDotL + sqrt(alpha2 + (1.0 - alpha2) * nDotL * nDotL));
    let attenuationV = 2.0 * nDotV / (nDotV + sqrt(alpha2 + (1.0 - alpha2) * nDotV * nDotV));
    let g = attenuationL * attenuationV;
    let dDenominator = (nDotH * alpha2 - nDotH) * nDotH + 1.0;
    let d = alpha2 / (PI * dDenominator * dDenominator);
    let reflectance90 = clamp(max(max(f0.r, f0.g), f0.b) * 25.0, 0.0, 1.0);
    let f = schlick(f0, vec3<f32>(reflectance90), vDotH);
    let burley90 = 2.0 * lDotH * lDotH * alpha - 0.5;
    let diffuse = diffuseColor / PI * (1.0 + burley90 * pow(1.0 - nDotL, 5.0))
        * (1.0 + burley90 * pow(1.0 - nDotV, 5.0));
    return nDotL * ((1.0 - f) * diffuse + f * g * d / (4.0 * nDotL * nDotV));
}

@fragment
fn fs_main(input: VertexOutput, @builtin(front_facing) frontFacing: bool) -> @location(0) vec4<f32> {
    let baseColor = textureSample(baseColorTexture, baseColorSampler, uv(input, material.textureCoordinates.x)) * material.baseColor * input.color;
    let mr = textureSample(metallicRoughnessTexture, metallicRoughnessSampler, uv(input, material.textureCoordinates.y));
    let normalUV = uv(input, material.textureCoordinates.z);
    let normalSample = textureSample(normalTexture, normalSampler, normalUV).xyz;
    let ao = textureSample(occlusionTexture, occlusionSampler, uv(input, material.textureCoordinates.w)).r;
    let emissive = textureSample(emissiveTexture, emissiveSampler, uv(input, material.flags.x)).rgb * material.emissiveAlphaCutoff.rgb;
    var n = perturbNormal(normalize(input.normal), input.worldPosition, normalUV, normalSample);
    n *= select(-1.0, 1.0, frontFacing);
    if (material.flags.y == 1u && baseColor.a < material.emissiveAlphaCutoff.a) {
        discard;
    }
    let roughness = clamp(mr.g * material.metallicRoughnessNormalOcclusion.y, 0.04, 1.0);
    let metallic = clamp(mr.b * material.metallicRoughnessNormalOcclusion.x, 0.0, 1.0);
    let diffuseColor = baseColor.rgb * (1.0 - metallic);
    let f0 = mix(vec3<f32>(0.04), baseColor.rgb, metallic);
    let v = normalize(frame.cameraPosition.xyz - input.worldPosition);
    let nDotV = clamp(abs(dot(n, v)), 0.001, 1.0);
    let reflection = reflect(-v, n);
    // GenerateBrdfLut stores roughness = 1 - rowCoordinate.
    let ab = textureSampleLevel(brdfLut, environmentSampler, vec2<f32>(nDotV, 1.0 - roughness), 0.0).rg;
    let fresnelRoughness = max(vec3<f32>(1.0 - roughness), f0) - f0;
    let kS = f0 + fresnelRoughness * pow(1.0 - nDotV, 5.0);
    let fssEss = kS * ab.x + ab.y;
    let lod = roughness * f32(textureNumLevels(prefilteredEnvironment) - 1u);
    let specular = textureSampleLevel(prefilteredEnvironment, environmentSampler, reflection, lod).rgb * fssEss;
    let irradiance = textureSampleLevel(irradianceEnvironment, environmentSampler, n, 0.0).rgb;
    let ems = 1.0 - (ab.x + ab.y);
    let fAverage = f0 + (1.0 - f0) / 21.0;
    let fmsEms = ems * fssEss * fAverage / (1.0 - fAverage * ems);
    let kD = diffuseColor * (1.0 - fssEss + fmsEms);
    let diffuse = (fmsEms + kD) * irradiance;
    let occlusion = mix(1.0, select(ao, 1.0, ao < 0.01), material.metallicRoughnessNormalOcclusion.w);
    var color = (specular + diffuse + directLight(n, v, input.worldPosition, diffuseColor, f0, roughness, nDotV)) * occlusion + emissive;
    // Avoid double gamma when an sRGB swapchain performs the encoding itself.
    if (material.flags.z == 0u) {
        color = pow(max(color, vec3<f32>(0.0)), vec3<f32>(1.0 / 2.2));
    }
    return vec4<f32>(color, select(1.0, baseColor.a, material.flags.y == 2u));
}
