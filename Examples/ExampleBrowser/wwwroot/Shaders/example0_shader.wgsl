struct VertexOutputs {
    //The position of the vertex
    @builtin(position) position: vec4<f32>,
    //The texture cooridnate of the vertex
    @location(0) tex_coord: vec2<f32>,
    @location(1) color: vec3<f32>
}

@group(0) @binding(0) var<uniform> projection_matrix: mat4x4<f32>;

@vertex
fn vs_main(
    @location(0) pos: vec2<f32>,
    @location(1) tex_coord: vec2<f32>,
    @location(2) color: vec3<f32>
) -> VertexOutputs {
    var output: VertexOutputs;

    output.position = projection_matrix * vec4<f32>(pos, 0.0, 1.0);
    output.tex_coord = tex_coord;
    output.color = color;

    return output;
}

@fragment
fn fs_main(input: VertexOutputs) -> @location(0) vec4<f32> {
    return vec4<f32>(input.color, 1.0);
}
