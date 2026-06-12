struct PerFrameData {
    model: mat4x4<f32>,
    normal: mat4x4<f32>,
    view: mat4x4<f32>,
    proj: mat4x4<f32>,
    camera_pos: vec4<f32>,
};

@group(0) @binding(0) var<uniform> pc: PerFrameData;
@group(0) @binding(1) var tex_cube: texture_cube<f32>;
@group(0) @binding(2) var tex_sampler: sampler;

const POSITIONS: array<vec3<f32>, 8> = array<vec3<f32>, 8>(
    vec3<f32>(-1.0, -1.0, 1.0),
    vec3<f32>(1.0, -1.0, 1.0),
    vec3<f32>(1.0, 1.0, 1.0),
    vec3<f32>(-1.0, 1.0, 1.0),
    vec3<f32>(-1.0, -1.0, -1.0),
    vec3<f32>(1.0, -1.0, -1.0),
    vec3<f32>(1.0, 1.0, -1.0),
    vec3<f32>(-1.0, 1.0, -1.0),
);

const INDICES: array<u32, 36> = array<u32, 36>(
    0u, 1u, 2u, 2u, 3u, 0u,
    1u, 5u, 6u, 6u, 2u, 1u,
    7u, 6u, 5u, 5u, 4u, 7u,
    4u, 0u, 3u, 3u, 7u, 4u,
    4u, 5u, 1u, 1u, 0u, 4u,
    3u, 2u, 6u, 6u, 7u, 3u
);

struct VsOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) dir: vec3<f32>,
};

@vertex
fn vs_main(@builtin(vertex_index) vertex_index: u32) -> VsOutput {
    var out: VsOutput;
    let idx = INDICES[vertex_index];
    let pos = POSITIONS[idx] * 100.0;

    let view_no_translation = mat4x4<f32>(
        vec4<f32>(pc.view[0].xyz, 0.0),
        vec4<f32>(pc.view[1].xyz, 0.0),
        vec4<f32>(pc.view[2].xyz, 0.0),
        vec4<f32>(0.0, 0.0, 0.0, 1.0)
    );

    out.position = pc.proj * view_no_translation * vec4<f32>(pos, 1.0);
    out.dir = pos;
    return out;
}

@fragment
fn fs_main(input: VsOutput) -> @location(0) vec4<f32> {
    return textureSample(tex_cube, tex_sampler, input.dir);
}