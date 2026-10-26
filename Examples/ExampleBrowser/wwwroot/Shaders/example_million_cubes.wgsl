struct PerFrameData {
    viewProjection: mat4x4<f32>,
    time: vec4<f32>,
};

@group(0) @binding(0) var<uniform> perFrame: PerFrameData;
// xyz: center, w: initial rotation angle. One vec4 per instance.
@group(0) @binding(1) var<storage, read> centers: array<vec4<f32>>;
@group(0) @binding(2) var xorTexture: texture_2d<f32>;
@group(0) @binding(3) var xorSampler: sampler;

const indices = array<u32, 36>(
    0, 2, 1, 2, 3, 1, 5, 4, 1, 1, 4, 0,
    0, 4, 6, 0, 6, 2, 6, 5, 7, 6, 4, 5,
    2, 6, 3, 6, 7, 3, 7, 1, 3, 7, 5, 1
);

const colors = array<vec3<f32>, 7>(
    vec3<f32>(1, 0, 0), vec3<f32>(0, 1, 0), vec3<f32>(0, 0, 1),
    vec3<f32>(1, 1, 0), vec3<f32>(0, 1, 1), vec3<f32>(1, 0, 1),
    vec3<f32>(1, 1, 1)
);

struct VertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) color: vec3<f32>,
    @location(1) uv: vec2<f32>,
};

@vertex
fn vs_main(@builtin(vertex_index) vertex: u32, @builtin(instance_index) instance: u32) -> VertexOutput {
    let center = centers[instance];
    let index = indices[vertex];
    let xyz = vec3<f32>(f32(index & 1u), f32((index & 4u) >> 2u), f32((index & 2u) >> 1u));
    let local = xyz - vec3<f32>(0.5);

    // Rodrigues' formula: rotate a unit cube about its (1, 1, 1) diagonal.
    let axis = normalize(vec3<f32>(1.0));
    let angle = perFrame.time.x + center.w;
    let c = cos(angle);
    let s = sin(angle);
    let rotated = local * c + cross(axis, local) * s + axis * dot(axis, local) * (1.0 - c);

    var output: VertexOutput;
    output.position = perFrame.viewProjection * vec4<f32>(rotated + center.xyz, 1.0);
    let face = vertex / 6u;
    if (face == 0u || face == 3u) {
        output.uv = xyz.xz;
    } else if (face == 1u || face == 4u) {
        output.uv = xyz.xy;
    } else {
        output.uv = xyz.yz;
    }
    output.color = colors[instance % 7u];
    return output;
}

@fragment
fn fs_main(input: VertexOutput) -> @location(0) vec4<f32> {
    return textureSample(xorTexture, xorSampler, input.uv) * vec4<f32>(input.color, 1.0);
}
