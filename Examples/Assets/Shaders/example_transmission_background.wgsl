// Present the linear opaque capture, with output encoding only at the final surface.
@group(0) @binding(0) var background: texture_2d<f32>;
@group(0) @binding(1) var backgroundSampler: sampler;
struct Output { @builtin(position) position: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex
fn vs_main(@builtin(vertex_index) index: u32) -> Output {
    let positions = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
    let p = positions[index];
    return Output(vec4<f32>(p, 0.0, 1.0), p * vec2<f32>(0.5, -0.5) + 0.5);
}
@fragment
fn fs_linear(input: Output) -> @location(0) vec4<f32> {
    return textureSampleLevel(background, backgroundSampler, input.uv, 0.0);
}
@fragment
fn fs_gamma(input: Output) -> @location(0) vec4<f32> {
    let color = textureSampleLevel(background, backgroundSampler, input.uv, 0.0);
    return vec4<f32>(pow(max(color.rgb, vec3<f32>(0.0)), vec3<f32>(1.0 / 2.2)), color.a);
}
