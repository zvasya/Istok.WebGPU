// Chapter07/03_Transmission: opaque HDR capture plus screen-space rough transmission.
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
    transmission: vec4<f32>, // factor, IOR, thickness, attenuation distance (0 = infinity)
    attenuationColor: vec4<f32>,
    // base color, metallic/roughness, normal, occlusion UV sets
    textureCoordinates: vec4<u32>,
    // emissive UV, alpha mode, sRGB render target, unused
    flags: vec4<u32>,
    transmissionUV: vec4<u32>,
};
@group(0) @binding(0) var<uniform> frame: PerFrameData;
@group(0) @binding(1) var<uniform> material: Material;
struct TextureTransform { u: vec4<f32>, v: vec4<f32> };
struct TextureTransforms { maps: array<TextureTransform, 7> };
@group(0) @binding(2) var<uniform> transforms: TextureTransforms;

@group(1) @binding(0) var baseColorTexture: texture_2d<f32>;
@group(1) @binding(1) var metallicRoughnessTexture: texture_2d<f32>;
@group(1) @binding(2) var normalTexture: texture_2d<f32>;
@group(1) @binding(3) var occlusionTexture: texture_2d<f32>;
@group(1) @binding(4) var emissiveTexture: texture_2d<f32>;
@group(1) @binding(7) var baseColorSampler: sampler;
@group(1) @binding(8) var metallicRoughnessSampler: sampler;
@group(1) @binding(9) var normalSampler: sampler;
@group(1) @binding(10) var occlusionSampler: sampler;
@group(1) @binding(11) var emissiveSampler: sampler;

@group(1) @binding(5) var transmissionTexture: texture_2d<f32>;
@group(1) @binding(6) var thicknessTexture: texture_2d<f32>;
@group(1) @binding(12) var transmissionSampler: sampler;
@group(1) @binding(13) var thicknessSampler: sampler;

@group(2) @binding(0) var prefilteredEnvironment: texture_cube<f32>;
@group(2) @binding(1) var irradianceEnvironment: texture_cube<f32>;
@group(2) @binding(2) var brdfLut: texture_2d<f32>;
@group(2) @binding(3) var environmentSampler: sampler;
@group(3) @binding(0) var opaqueBackground: texture_2d<f32>;
@group(3) @binding(1) var backgroundSampler: sampler;

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

struct Lighting { diffuse: vec3<f32>, specular: vec3<f32> };

fn directLight(n: vec3<f32>, v: vec3<f32>, worldPosition: vec3<f32>,
    diffuseColor: vec3<f32>, f0: vec3<f32>, roughness: f32, nDotV: f32) -> Lighting {
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
    return Lighting(nDotL * (1.0 - f) * diffuse, f * g * d / (4.0 * nDotV));
}

struct Surface {
    diffuse: vec3<f32>, specular: vec3<f32>, baseColor: vec3<f32>, f0: vec3<f32>,
    n: vec3<f32>, v: vec3<f32>, roughness: f32, metallic: f32,
    transmission: f32, thickness: f32, alpha: f32,
};
fn surface(input: VertexOutput, frontFacing: bool) -> Surface {
    let baseColor = textureSample(baseColorTexture, baseColorSampler, uv(input, material.textureCoordinates.x, 0u)) * material.baseColor * input.color;
    let mr = textureSample(metallicRoughnessTexture, metallicRoughnessSampler, uv(input, material.textureCoordinates.y, 1u));
    let normalUV = uv(input, material.textureCoordinates.z, 2u);
    let normalSample = textureSample(normalTexture, normalSampler, normalUV).xyz;
    let ao = textureSample(occlusionTexture, occlusionSampler, uv(input, material.textureCoordinates.w, 3u)).r;
    let emissive = textureSample(emissiveTexture, emissiveSampler, uv(input, material.flags.x, 4u)).rgb * material.emissiveAlphaCutoff.rgb;
    var n = perturbNormal(normalize(input.normal), input.worldPosition, normalUV, normalSample);
    n *= select(-1.0, 1.0, frontFacing);
    let roughness = clamp(mr.g * material.metallicRoughnessNormalOcclusion.y, 0.04, 1.0);
    let metallic = clamp(mr.b * material.metallicRoughnessNormalOcclusion.x, 0.0, 1.0);
    let diffuseColor = baseColor.rgb * (1.0 - metallic);
    let dielectricF0 = pow((material.transmission.y - 1.0) / (material.transmission.y + 1.0), 2.0);
    let f0 = mix(vec3<f32>(dielectricF0), baseColor.rgb, metallic);
    let v = normalize(frame.cameraPosition.xyz - input.worldPosition);
    let nDotV = clamp(abs(dot(n, v)), 0.001, 1.0);
    let reflection = reflect(-v, n);
    // GenerateBrdfLut stores roughness = 1 - rowCoordinate.
    let ab = textureSampleLevel(brdfLut, environmentSampler, vec2<f32>(nDotV, 1.0 - roughness), 0.0).rg;
    let fresnelRoughness = max(vec3<f32>(1.0 - roughness), f0) - f0;
    let kS = f0 + fresnelRoughness * pow(1.0 - nDotV, 5.0);
    let fssEss = select(vec3<f32>(0.0), kS * ab.x + ab.y, max(max(f0.r, f0.g), f0.b) > 1e-7);
    let lod = roughness * f32(textureNumLevels(prefilteredEnvironment) - 1u);
    let specular = textureSampleLevel(prefilteredEnvironment, environmentSampler, reflection, lod).rgb * fssEss;
    let irradiance = textureSampleLevel(irradianceEnvironment, environmentSampler, n, 0.0).rgb;
    let ems = 1.0 - (ab.x + ab.y);
    let fAverage = f0 + (1.0 - f0) / 21.0;
    let fmsEms = ems * fssEss * fAverage / (1.0 - fAverage * ems);
    let kD = diffuseColor * (1.0 - fssEss + fmsEms);
    let diffuse = (fmsEms + kD) * irradiance;
    let occlusion = mix(1.0, select(ao, 1.0, ao < 0.01), material.metallicRoughnessNormalOcclusion.w);
    let direct = directLight(n, v, input.worldPosition, diffuseColor, f0, roughness, nDotV);
    let factor = clamp(material.transmission.x * textureSample(transmissionTexture, transmissionSampler,
        uv(input, material.transmissionUV.x, 5u)).r, 0.0, 1.0);
    let thickness = material.transmission.z * textureSample(thicknessTexture, thicknessSampler,
        uv(input, material.transmissionUV.y, 6u)).g;
    if (material.flags.y == 1u && baseColor.a < material.emissiveAlphaCutoff.a) { discard; }
    return Surface((diffuse + direct.diffuse) * occlusion, (specular + direct.specular) * occlusion + emissive,
        baseColor.rgb, f0, n, v, roughness, metallic, factor, thickness,
        select(1.0, baseColor.a, material.flags.y == 2u));
}

@fragment
fn fs_opaque(input: VertexOutput, @builtin(front_facing) frontFacing: bool) -> @location(0) vec4<f32> {
    let s = surface(input, frontFacing);
    // Captured as linear RGBA16F. No dependency on the background texture here.
    return vec4<f32>(s.diffuse + s.specular, s.alpha);
}

fn directTransmission(s: Surface, position: vec3<f32>) -> vec3<f32> {
    let l = normalize(vec3<f32>(0.0, 0.0, -5.0) - position);
    let nl = max(dot(-s.n, l), 0.0);
    if (nl <= 0.0 || material.transmission.y <= 1.00001) { return vec3<f32>(0.0); }
    let mirroredL = normalize(l - 2.0 * s.n * dot(l, s.n));
    let h = normalize(mirroredL + s.v);
    let nv = max(dot(s.n, s.v), 0.001);
    let nh = max(dot(s.n, h), 0.0);
    let r = max(s.roughness * clamp(2.0 * material.transmission.y - 2.0, 0.0, 1.0), 0.04);
    let a = r * r;
    let a2 = a * a;
    let denom = nh * nh * (a2 - 1.0) + 1.0;
    let d = a2 / max(PI * denom * denom, 1e-12);
    let visibility = 0.5 / max(nl * sqrt(nv * nv * (1.0 - a2) + a2)
        + nv * sqrt(nl * nl * (1.0 - a2) + a2), 1e-6);
    let f = schlick(s.f0, vec3<f32>(1.0), max(dot(s.v, h), 0.0));
    return nl * (1.0 - f) * s.baseColor * (1.0 - s.metallic) * d * visibility;
}

@fragment
fn fs_transmission(input: VertexOutput, @builtin(front_facing) frontFacing: bool) -> @location(0) vec4<f32> {
    let s = surface(input, frontFacing);
    let ior = material.transmission.y;
    let scale = vec3<f32>(length(frame.model[0].xyz), length(frame.model[1].xyz), length(frame.model[2].xyz));
    let ray = refract(-s.v, s.n, 1.0 / ior) * s.thickness * scale;
    let exitClip = frame.viewProjection * vec4<f32>(input.worldPosition + ray, 1.0);
    // WebGPU's image origin is top-left; projection Y points upward.
    let coordinate = exitClip.xy / exitClip.w * vec2<f32>(0.5, -0.5) + 0.5;
    let lod = s.roughness * clamp(2.0 * ior - 2.0, 0.0, 1.0)
        * f32(textureNumLevels(opaqueBackground) - 1u);
    let behind = textureSampleLevel(opaqueBackground, backgroundSampler, coordinate, lod).rgb;
    var attenuation = vec3<f32>(1.0);
    if (material.transmission.w > 0.0) {
        attenuation = pow(max(material.attenuationColor.rgb, vec3<f32>(1e-6)),
            vec3<f32>(length(ray) / material.transmission.w));
    }
    let nv = clamp(abs(dot(s.n, s.v)), 0.001, 1.0);
    let ab = textureSampleLevel(brdfLut, environmentSampler, vec2<f32>(nv, 1.0 - s.roughness), 0.0).rg;
    let reflection = select(vec3<f32>(0.0), s.f0 * ab.x + ab.y, max(max(s.f0.r, s.f0.g), s.f0.b) > 1e-7);
    let transmitted = behind * attenuation * s.baseColor * (1.0 - s.metallic) * (1.0 - reflection)
        + directTransmission(s, input.worldPosition);
    var color = s.specular + mix(s.diffuse, transmitted, s.transmission);
    if (material.flags.z == 0u) { color = pow(max(color, vec3<f32>(0.0)), vec3<f32>(1.0 / 2.2)); }
    return vec4<f32>(color, s.alpha);
}
