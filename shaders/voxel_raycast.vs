#version 300 es
in vec3 p;
uniform mat4 proj;
uniform mat4 c2w;
out vec3 ray_pos;
out vec3 ray_dir;
// out vec3 forward;

void main()
{
    gl_Position = proj * vec4(p,1);
	vec4 rp = c2w * vec4(0,0,0,1);
	ray_pos = rp.xyz;
	vec4 rd = c2w * vec4(p,1);
	// forward = (c2w * vec4(0,0,-1,0)).xyz;

	ray_dir = rd.xyz - ray_pos;
}