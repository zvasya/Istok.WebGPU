struct PerFrameData {
    model: mat4x4<f32>,
    normal: mat4x4<f32>,
    view: mat4x4<f32>,
    proj: mat4x4<f32>,
    camera_pos: vec4<f32>,
};

@group(0) @binding(0) var<uniform> pc: PerFrameData;
@group(0) @binding(1) var tex_2d: texture_2d<f32>;
@group(0) @binding(2) var tex_cube: texture_cube<f32>;
@group(0) @binding(3) var tex_sampler: sampler;

struct VsInput {
    @location(0) pos: vec3<f32>,
    @location(1) normal: vec3<f32>,
    @location(2) uv: vec2<f32>,
};

struct VsOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) uv: vec2<f32>,
    @location(1) world_normal: vec3<f32>,
    @location(2) world_pos: vec3<f32>,
};

@vertex
fn vs_main(input: VsInput) -> VsOutput {
    var out: VsOutput;

    let world_pos4 = pc.model * vec4<f32>(input.pos, 1.0);

    out.position = pc.proj * pc.view * world_pos4;
    out.uv = input.uv;
    out.world_normal = (pc.normal * vec4<f32>(input.normal, 0.0)).xyz;
    out.world_pos = world_pos4.xyz;
    return out;
}

@fragment
fn fs_main(input: VsOutput) -> @location(0) vec4<f32> {
    
    let n = normalize(input.world_normal);
    let v = normalize(pc.camera_pos.xyz - input.world_pos);
    let reflection = -normalize(reflect(v, n));
    let color_refl = textureSample(tex_cube, tex_sampler, reflection);
    let ka = color_refl * 0.6;

    let ndotl = clamp(dot(n, normalize(vec3<f32>(0.0, 0.0, -1.0))), 0.1, 1.0);
    let kd = textureSample(tex_2d, tex_sampler, input.uv) * ndotl;
    return ka + kd;
}
