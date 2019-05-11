float CreateSphere(float3 p, float s)
{
	return length(p) - s;
}

float CreatePlane(float3 position, float4 planeNormalWithOffset)
{
	return dot(position, planeNormalWithOffset.xyz) + planeNormalWithOffset.w;
}

float CreateBox(float3 position, float3 size)
{
	float3 distance = abs(position) - size;
	return min(max(distance.x, max(distance.y, distance.z)), 0.0) + length(max(distance, 0.0));
}

float CreateRoundBox(in float3 position, in float3 size, in float radius)
{
	float3 distance = abs(position) - size;
	return min(max(distance.x, max(distance.y, distance.z)), 0.0) + length(max(distance, 0.0)) - radius;
}

// BOOLEAN OPERATORS //

// Union
float Union(float d1, float d2)
{
	return min(d1, d2);
}

// Subtraction
float Subtraction(float d1, float d2)
{
	return max(-d1, d2);
}

// Intersection
float Intersection(float d1, float d2)
{
	return max(d1, d2);
}

// SMOOTH BOOLEAN OPERATORS

float SmoothUnion(float d1, float d2, float k)
{
	float h = clamp(0.5 + 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) - k * h*(1.0 - h);
}

float SmoothSubtraction(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 + d1) / k, 0.0, 1.0);
	return lerp(d2, -d1, h) + k * h*(1.0 - h);
}

float SmoothIntersection(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) + k * h*(1.0 - h);
}

float Modulator(inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(-p + halfsize, size) - halfsize;
	return c;
}