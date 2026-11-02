// Chapter07/06_Specular: colored dielectric specular reflection together with Charlie sheen.
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
    sheen: vec4<f32>, // color RGB, perceptual roughness
    specular: vec4<f32>, // linear specular color RGB, strength
    optics: vec4<f32>, // IOR
    // base color, metallic/roughness, normal, occlusion UV sets
    textureCoordinates: vec4<u32>,
    // emissive UV, alpha mode, sRGB render target, unused
    flags: vec4<u32>,
    effectUV: vec4<u32>,
};
@group(0) @binding(0) var<uniform> frame: PerFrameData;
@group(0) @binding(1) var<uniform> material: Material;
struct TextureTransform { u: vec4<f32>, v: vec4<f32> };
struct TextureTransforms { maps: array<TextureTransform, 9> };
@group(0) @binding(2) var<uniform> transforms: TextureTransforms;

@group(1) @binding(0) var baseColorTexture: texture_2d<f32>;
@group(1) @binding(1) var metallicRoughnessTexture: texture_2d<f32>;
@group(1) @binding(2) var normalTexture: texture_2d<f32>;
@group(1) @binding(3) var occlusionTexture: texture_2d<f32>;
@group(1) @binding(4) var emissiveTexture: texture_2d<f32>;
@group(1) @binding(9) var baseColorSampler: sampler;
@group(1) @binding(10) var metallicRoughnessSampler: sampler;
@group(1) @binding(11) var normalSampler: sampler;
@group(1) @binding(12) var occlusionSampler: sampler;
@group(1) @binding(13) var emissiveSampler: sampler;

@group(1) @binding(5) var sheenColorTexture: texture_2d<f32>;
@group(1) @binding(6) var sheenRoughnessTexture: texture_2d<f32>;
@group(1) @binding(14) var sheenColorSampler: sampler;
@group(1) @binding(15) var sheenRoughnessSampler: sampler;

@group(1) @binding(7) var specularTexture: texture_2d<f32>;
@group(1) @binding(8) var specularColorTexture: texture_2d<f32>;
@group(1) @binding(16) var specularSampler: sampler;
@group(1) @binding(17) var specularColorSampler: sampler;

@group(2) @binding(0) var prefilteredEnvironment: texture_cube<f32>;
@group(2) @binding(1) var irradianceEnvironment: texture_cube<f32>;
@group(2) @binding(2) var brdfLut: texture_2d<f32>;
@group(2) @binding(3) var environmentSampler: sampler;
@group(2) @binding(4) var charlieEnvironment: texture_cube<f32>;

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

fn uv(input: VertexOutput, index: u32, map: u32) -> vec2<f32> {
    let coordinate = vec3<f32>(select(input.uv0uv1.xy, input.uv0uv1.zw, index == 1u), 1.0);
    return vec2<f32>(dot(transforms.maps[map].u.xyz, coordinate), dot(transforms.maps[map].v.xyz, coordinate));
}

fn perturbNormal(normal: vec3<f32>, position: vec3<f32>, coordinate: vec2<f32>, sample: vec3<f32>) -> vec3<f32> {
    let dp1 = dpdx(position);
    let dp2 = dpdy(position);
    let duv1 = dpdx(coordinate);
    let duv2 = dpdy(coordinate);
    let dp2perp = cross(dp2, normal);
    let dp1perp = cross(normal, dp1);
    let tangent = dp2perp * duv1.x + dp1perp * duv2.x;
    let bitangent = dp2perp * duv1.y + dp1perp * duv2.y;
    let inverseScale = inverseSqrt(max(max(dot(tangent, tangent), dot(bitangent, bitangent)), 1e-20));
    var map = sample * 2.0 - 1.0;
    map = vec3<f32>(map.xy * material.metallicRoughnessNormalOcclusion.z, map.z);
    return normalize(mat3x3<f32>(tangent * inverseScale, bitangent * inverseScale, normal) * normalize(map));
}

fn schlick(f0: vec3<f32>, f90: vec3<f32>, cosine: f32) -> vec3<f32> {
    return f0 + (f90 - f0) * pow(clamp(1.0 - cosine, 0.0, 1.0), 5.0);
}

fn directLight(n: vec3<f32>, v: vec3<f32>, worldPosition: vec3<f32>,
    diffuseColor: vec3<f32>, f0: vec3<f32>, f90: vec3<f32>, roughness: f32, nDotV: f32) -> vec3<f32> {
    let light = normalize(vec3<f32>(0.0, 0.0, -5.0) - worldPosition);
    let halfVector = normalize(light + v);
    let nDotL = clamp(dot(n, light), 0.001, 1.0);
    let nDotH = clamp(dot(n, halfVector), 0.0, 1.0);
    let vDotH = clamp(dot(v, halfVector), 0.0, 1.0);
    let alpha = roughness * roughness;
    let alpha2 = alpha * alpha;
    let attenuationL = 2.0 * nDotL / (nDotL + sqrt(alpha2 + (1.0 - alpha2) * nDotL * nDotL));
    let attenuationV = 2.0 * nDotV / (nDotV + sqrt(alpha2 + (1.0 - alpha2) * nDotV * nDotV));
    let g = attenuationL * attenuationV;
    let dDenominator = (nDotH * alpha2 - nDotH) * nDotH + 1.0;
    let d = alpha2 / (PI * dDenominator * dDenominator);
    let f = schlick(f0, f90, vDotH);
    // Scalar remaining energy avoids inverse-colored diffuse reflections.
    let diffuse = diffuseColor / PI * (1.0 - max(max(f.r, f.g), f.b));
    return nDotL * diffuse + f * g * d / (4.0 * nDotV);

}

// Charlie distribution and the fitted visibility term used by the cookbook.
fn distributionCharlie(roughness: f32, nh: f32) -> f32 {
    let invR = 1.0 / (roughness * roughness);
    let sin2 = max(1.0 - nh * nh, 0.0);
    return (2.0 + invR) * pow(sin2, invR * 0.5) / (2.0 * PI);
}
fn sheenFit(x: f32, alpha: f32) -> f32 {
    let t = (1.0 - alpha) * (1.0 - alpha);
    let a = mix(21.5473, 25.3245, t);
    let b = mix(3.82987, 3.32435, t);
    let c = mix(0.19823, 0.16801, t);
    let d = mix(-1.97760, -1.27393, t);
    let e = mix(-4.32054, -4.85967, t);
    return a / (1.0 + b * pow(max(x, 0.0), c)) + d * x + e;
}
fn lambdaSheen(cosine: f32, alpha: f32) -> f32 {
    if (cosine < 0.5) { return exp(sheenFit(cosine, alpha)); }
    return exp(2.0 * sheenFit(0.5, alpha) - sheenFit(1.0 - cosine, alpha));
}
fn directSheen(color: vec3<f32>, roughness: f32, nl: f32, nv: f32, nh: f32) -> vec3<f32> {
    let alpha = roughness * roughness;
    let visibility = clamp(1.0 / max((1.0 + lambdaSheen(nv, alpha) + lambdaSheen(nl, alpha))
        * 4.0 * nv * nl, 1e-6), 0.0, 1.0);
    return color * nl * distributionCharlie(roughness, nh) * visibility;
}
// Approximation from the cookbook: reduce the base response beneath the sheen.
fn sheenAlbedo(cosine: f32, roughness: f32) -> f32 {
    let c = 1.0 - cosine;
    return 0.65584461 * c * c * c
        + 1.0 / (4.16526551 + exp(-7.97291361 * sqrt(roughness) + 6.33516894));
}

@fragment
fn fs_main(input: VertexOutput, @builtin(front_facing) frontFacing: bool) -> @location(0) vec4<f32> {
    let baseColor = textureSample(baseColorTexture, baseColorSampler, uv(input, material.textureCoordinates.x, 0u)) * material.baseColor * input.color;
    let mr = textureSample(metallicRoughnessTexture, metallicRoughnessSampler, uv(input, material.textureCoordinates.y, 1u));
    let normalUV = uv(input, material.textureCoordinates.z, 2u);
    let normalSample = textureSample(normalTexture, normalSampler, normalUV).xyz;
    let ao = textureSample(occlusionTexture, occlusionSampler, uv(input, material.textureCoordinates.w, 3u)).r;
    let emissive = textureSample(emissiveTexture, emissiveSampler, uv(input, material.flags.x, 4u)).rgb * material.emissiveAlphaCutoff.rgb;
    let sheenColor = material.sheen.rgb * textureSample(sheenColorTexture, sheenColorSampler,
        uv(input, material.effectUV.x, 5u)).rgb;
    // The roughness map stores sheen roughness in alpha, independently of sRGB color.
    let sheenRoughness = clamp(material.sheen.a * textureSample(sheenRoughnessTexture, sheenRoughnessSampler,
        uv(input, material.effectUV.y, 6u)).a, 0.04, 1.0);
    // Strength is linear alpha; color is RGB decoded from an sRGB texture.
    let specularStrength = clamp(material.specular.a * textureSample(specularTexture, specularSampler,
        uv(input, material.effectUV.z, 7u)).a, 0.0, 1.0);
    let specularColor = material.specular.rgb * textureSample(specularColorTexture, specularColorSampler,
        uv(input, material.effectUV.w, 8u)).rgb;
    var n = perturbNormal(normalize(input.normal), input.worldPosition, normalUV, normalSample);
    n *= select(-1.0, 1.0, frontFacing);
    if (material.flags.y == 1u && baseColor.a < material.emissiveAlphaCutoff.a) {
        discard;
    }
    let roughness = clamp(mr.g * material.metallicRoughnessNormalOcclusion.y, 0.04, 1.0);
    let metallic = clamp(mr.b * material.metallicRoughnessNormalOcclusion.x, 0.0, 1.0);
    let diffuseColor = baseColor.rgb * (1.0 - metallic);
    let iorF0 = pow((material.optics.x - 1.0) / (material.optics.x + 1.0), 2.0);
    // Clamp F0 after applying color, before applying strength. Colors may exceed 1.
    let dielectricF0 = min(vec3<f32>(iorF0) * specularColor, vec3<f32>(1.0)) * specularStrength;
    let f0 = mix(dielectricF0, baseColor.rgb, metallic);
    let f90 = vec3<f32>(mix(specularStrength, 1.0, metallic));
    let v = normalize(frame.cameraPosition.xyz - input.worldPosition);
    let nDotV = clamp(abs(dot(n, v)), 0.001, 1.0);
    let reflection = reflect(-v, n);
    // GenerateBrdfLut stores roughness = 1 - rowCoordinate.
    let ab = textureSampleLevel(brdfLut, environmentSampler, vec2<f32>(nDotV, 1.0 - roughness), 0.0).rg;
    let fresnelRoughness = max(f90 * (1.0 - roughness), f0) - f0;
    let kS = f0 + fresnelRoughness * pow(1.0 - nDotV, 5.0);
    let fssEss = kS * ab.x + f90 * ab.y;
    let lod = roughness * f32(textureNumLevels(prefilteredEnvironment) - 1u);
    let specular = textureSampleLevel(prefilteredEnvironment, environmentSampler, reflection, lod).rgb * fssEss;
    let irradiance = textureSampleLevel(irradianceEnvironment, environmentSampler, n, 0.0).rgb;
    let ems = 1.0 - (ab.x + ab.y);
    let fAverage = f0 + (f90 - f0) / 21.0;
    let fmsEms = ems * fssEss * fAverage / (1.0 - fAverage * ems);
    let kD = diffuseColor * clamp(1.0 - max(max((fssEss + fmsEms).r, (fssEss + fmsEms).g), (fssEss + fmsEms).b), 0.0, 1.0);
    let diffuse = (fmsEms + kD) * irradiance;
    let occlusion = mix(1.0, select(ao, 1.0, ao < 0.01), material.metallicRoughnessNormalOcclusion.w);
    let sheenWeight = max(max(sheenColor.r, sheenColor.g), sheenColor.b);
    let light = normalize(vec3<f32>(0.0, 0.0, -5.0) - input.worldPosition);
    let nl = max(dot(n, light), 0.0);
    let nh = clamp(dot(n, normalize(light + v)), 0.0, 1.0);
    let iblScale = clamp(1.0 - sheenWeight * sheenAlbedo(nDotV, sheenRoughness), 0.0, 1.0);
    let directScale = clamp(1.0 - sheenWeight * max(sheenAlbedo(nDotV, sheenRoughness),
        sheenAlbedo(nl, sheenRoughness)), 0.0, 1.0);
    let sheenLod = sheenRoughness * f32(textureNumLevels(charlieEnvironment) - 1u);
    // The blue LUT channel integrates Charlie; its rows also run from rough to smooth.
    let sheenBrdf = textureSampleLevel(brdfLut, environmentSampler,
        vec2<f32>(nDotV, 1.0 - sheenRoughness), 0.0).b;
    let sheenIbl = sheenColor * sheenBrdf
        * textureSampleLevel(charlieEnvironment, environmentSampler, reflection, sheenLod).rgb;
    let baseDirect = directLight(n, v, input.worldPosition, diffuseColor, f0, f90, roughness, nDotV);
    var color = ((specular + diffuse) * iblScale + baseDirect * directScale + sheenIbl
        + directSheen(sheenColor, sheenRoughness, nl, nDotV, nh)) * occlusion + emissive;
    // Avoid double gamma when an sRGB swapchain performs the encoding itself.
    if (material.flags.z == 0u) {
        color = pow(max(color, vec3<f32>(0.0)), vec3<f32>(1.0 / 2.2));
    }
    return vec4<f32>(color, select(1.0, baseColor.a, material.flags.y == 2u));
}
