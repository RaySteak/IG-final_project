#version 300 es

precision highp float;
precision highp int;
precision highp sampler3D;

#define TYPE_STOP -1
#define TYPE_REFRACTION 0
#define TYPE_REFLECTION 1

#define NUM_LIGHTS 1

in vec3 ray_pos;
in vec3 ray_dir;
// in vec3 forward;

out vec4 color;

struct Ray {
	vec3 pos;
	vec3 dir;
};

struct Material {
	vec3  k_d;	// diffuse coefficient
	vec3  k_s;	// specular coefficient
	float n;	// specular exponent
	bool is_refractive;
	float refraction_ind;
};

struct Sphere {
	vec3     center;
	float    radius;
	Material mtl;
};

struct Light {
	vec3 position;
	vec3 intensity;
};

struct HitInfo {
	int X, Y, Z;
	float    t;
	vec3     position;
	vec3     normal;
	Material material_in;
	Material material_hit;
	bool refracted;
	int cube_id_hit;
};

uniform Light  lights [ NUM_LIGHTS  ];
uniform samplerCube envMap;
uniform sampler2D bumpMap;
uniform sampler3D chunk;

Material get_material(int id)
{
	Material material;
	if (id == 1) {
		material.refraction_ind = 1.1;
		material.is_refractive = true;
		return material;
	} else if (id == 2) {
		material.is_refractive = false;
		material.k_s = vec3(1.0, 0.0, 0.0);
		return material;
	} else {
		material.is_refractive = true;
		material.refraction_ind = 1.0;
		return material;
	}
}

int get_cube_id(int x, int y, int z, inout Material material)
{
	if (x < 0 || y < 0 || z < 0 || x >= 16 || y >= 16 || z >= 16) {
		material.refraction_ind = 1.0;
		material.is_refractive = true;
		return 0;
	}

	int cubeId = int(texture(chunk, vec3(float(x) / 16.0, float(y) / 16.0, float(z) / 16.0)).r * 255.0);
	material = get_material(cubeId);

	return cubeId;
}

bool castRay(Ray ray, inout HitInfo hitInfo)
{
	const int render_distance = 20;
	Material material_prev, material_cur;


    int X = int(floor(ray.pos.x));
    int Y = int(floor(ray.pos.y));
    int Z = int(floor(ray.pos.z));

    int stepX = int(sign(ray.dir.x));
    int stepY = int(sign(ray.dir.y));
    int stepZ = int(sign(ray.dir.z));

    float tDeltaX = abs(1.0 / ray.dir.x);
    float tDeltaY = abs(1.0 / ray.dir.y);
    float tDeltaZ = abs(1.0 / ray.dir.z);

    float tMaxX = tDeltaX * (stepX > 0 ? 1.0 - fract(ray.pos.x) : fract(ray.pos.x));
	float tMaxY = tDeltaY * (stepY > 0 ? 1.0 - fract(ray.pos.y) : fract(ray.pos.y));
	float tMaxZ = tDeltaZ * (stepZ > 0 ? 1.0 - fract(ray.pos.z) : fract(ray.pos.z));

    bool hit = false;
    
    for (int i = 0; i < render_distance; i++) {
		int oldX = X;
		int oldY = Y;
		int oldZ = Z;

        if(tMaxX < tMaxY) {
            if(tMaxX < tMaxZ) {
                X = X + stepX;
				hitInfo.t = tMaxX;
				if (stepX > 0)
					hitInfo.normal = vec3(-1, 0, 0);
				else
					hitInfo.normal = vec3(1, 0, 0);
                tMaxX = tMaxX + tDeltaX;
            } else {
                Z = Z + stepZ;
				hitInfo.t = tMaxZ;
				if (stepZ > 0)
					hitInfo.normal = vec3(0, 0, -1);
				else
					hitInfo.normal = vec3(0, 0, 1);
                tMaxZ = tMaxZ + tDeltaZ;
            }
        } else {
            if(tMaxY < tMaxZ) {
                Y = Y + stepY;
				hitInfo.t = tMaxY;
				if (stepY > 0)
					hitInfo.normal = vec3(0, -1, 0);
				else
					hitInfo.normal = vec3(0, 1, 0);
                tMaxY = tMaxY + tDeltaY;
            } else {
                Z = Z + stepZ;
				hitInfo.t = tMaxZ;
				if (stepZ > 0)
					hitInfo.normal = vec3(0, 0, -1);
				else
					hitInfo.normal = vec3(0, 0, 1);
                tMaxZ = tMaxZ + tDeltaZ;
            }
        }
		// if passing through mediums with different refractive indices
		int cube_prev_id = get_cube_id(oldX, oldY, oldZ, hitInfo.material_in);
		int cube_cur_id = get_cube_id(X, Y, Z, hitInfo.material_hit);
		// HERE, SKIP GOING OUT OF CUBE
		// if (cube_prev_id != -1)
			// continue;
		if (hitInfo.material_hit.is_refractive) {
			if (hitInfo.material_in.refraction_ind != hitInfo.material_hit.refraction_ind) {
				hit = true;
				hitInfo.refracted = true;
				break;
			} else {
				continue;
			}
		}
        if (cube_cur_id != 0) {
            hit = true;
			hitInfo.refracted = false;
            break;
        }
    }

	hitInfo.position = ray.pos + ray.dir * hitInfo.t;

	if (hit) {
		vec2 bump_sample;
		if (hitInfo.normal.x != 0.0)
			bump_sample = hitInfo.position.yz;
		else if (hitInfo.normal.y != 0.0)
			bump_sample = hitInfo.position.xz;
		else
			bump_sample = hitInfo.position.xy;
		
		vec3 bump = texture(bumpMap, bump_sample).rgb;
		bump = bump * 2.0 - 1.0;

		if (hitInfo.normal.x != 0.0) {
			if (hitInfo.normal.x > 0.0)
				hitInfo.normal = vec3(bump.z, bump.x, bump.y);
			else
				hitInfo.normal = vec3(-bump.z, bump.x, bump.y);
		} else if (hitInfo.normal.y != 0.0) {
			if (hitInfo.normal.y > 0.0)
				hitInfo.normal = vec3(bump.x, bump.z, bump.y);
			else
				hitInfo.normal = vec3(bump.x, -bump.z, bump.y);
		} else {
			if (hitInfo.normal.z > 0.0)
				hitInfo.normal = vec3(bump.x, bump.y, bump.z);
			else
				hitInfo.normal = vec3(bump.x, bump.y, -bump.z);
		}
		hitInfo.normal = normalize(hitInfo.normal);
	}

	return hit;
}

vec3 skybox(vec3 dir)
{
	// return sin(dir * 3.14159265 * 0.001) * 0.5 + 0.5;
	return texture(envMap, dir).rgb;
}

vec3 TraceRayFurther(Ray ray, vec3 k_s)
{
	const float bias = 0.001; // TODO: define this somewhere else, unite with other definition
	const int bounceLimit = 5; // TODO: define this somewhere else

	vec3 color = vec3(0.0, 0.0, 0.0);
	for (int i = 0; i < bounceLimit; i++) {
		HitInfo hitInfo;
		int type_change = TYPE_STOP; // -1 for stop, 0 for refraction, 1 for reflection 
		bool hit = castRay(ray, hitInfo);
		if (!hit) {
			color += k_s * skybox(ray.dir);
			break;
		}
		if (hitInfo.refracted) {
			type_change = TYPE_REFRACTION;
			vec3 new_dir = refract(ray.dir, hitInfo.normal, hitInfo.material_in.refraction_ind / hitInfo.material_hit.refraction_ind);

			// Total internal reflection
			if (length(new_dir) == 0.0) {
				new_dir = reflect(ray.dir, hitInfo.normal);
				type_change = TYPE_REFLECTION;
			}
			ray.dir = new_dir;
		}
		if (type_change == TYPE_STOP) {
			color += k_s * hitInfo.material_hit.k_s;
			break;
		} else if (type_change == TYPE_REFRACTION) {
			ray.pos = hitInfo.position - hitInfo.normal * bias;
		} else {
			ray.pos = hitInfo.position + hitInfo.normal * bias;
		}
	}
	return color;
}

vec3 TraceRay(Ray ray)
{
	const float bias = 0.001; // TODO: define this somewhere else, unite with other definition
	const int bounceLimit = 5; // TODO: define this somewhere else

	vec3 k_s = vec3(1.0, 1.0, 1.0); // TODO: do something with this

	HitInfo hitInfo;
	bool hit = castRay(ray, hitInfo);
	if (!hit)
		return skybox(ray.dir);

	if (hitInfo.refracted) {
		float n1 = hitInfo.material_in.refraction_ind;
		float n2 = hitInfo.material_hit.refraction_ind;
		vec3 refracted = refract(ray.dir, hitInfo.normal, n1 / n2);
		vec3 reflected = reflect(ray.dir, hitInfo.normal);

		Ray reflected_ray;
		reflected_ray.pos = hitInfo.position + hitInfo.normal * bias;
		reflected_ray.dir = reflected;

		// Total internal reflection
		if (length(refracted) == 0.0) {
			return k_s * TraceRayFurther(reflected_ray, k_s);
		} else {
			refracted = normalize(refracted);
			// Implement Fresnel effect with Schlick approximation
			float R0 = pow((n1 - n2) / (n1 + n2), 2.0);
			float R = R0 + (1.0 - R0) * pow(1.0 - dot(normalize(-ray.dir), hitInfo.normal), 5.0);

			Ray refracted_ray;
			refracted_ray.pos = hitInfo.position - hitInfo.normal * bias;
			refracted_ray.dir = refracted;
			
			if (R == 0.0) // Only refraction
				return k_s * TraceRayFurther(refracted_ray, k_s);
			if (R == 1.0) // Only reflection
				return k_s * TraceRayFurther(reflected_ray, k_s);
			return k_s * mix(TraceRayFurther(refracted_ray, k_s), TraceRayFurther(reflected_ray, k_s), R);
		}
	}
	return hitInfo.material_hit.k_s;
}

vec4 RayTracer(Ray ray)
{
	const int num_passes = 1; // TODO: define this somewhere else

	// TODO: why does normalizing the direction screw up everything?
	ray.dir = normalize(ray.dir);
	
	vec3 color = TraceRay(ray);

	return vec4(color / float(num_passes), 1.0);
	// HitInfo hitInfo;
	// bool hit = castRay(ray, hitInfo);
	// if (hit) {
    //     return vec4(1.0, 0.0, 0.0, 1.0);
    // } else {
	// 	return vec4( textureCube( envMap, ray.dir.xzy ).rgb, 0 );
    // }
}

void main()
{
	// if (length(cross(normalize(ray_dir), forward)) < 0.01) {
	// 	color = vec4(0.0, 1.0, 0.0, 1.0);
	// 	return;
	// }

	Ray primary_ray;
	primary_ray.pos = ray_pos;
	primary_ray.dir = ray_dir;
	color = RayTracer(primary_ray);
}