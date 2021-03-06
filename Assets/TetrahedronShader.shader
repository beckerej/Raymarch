﻿Shader "PeerPlay/TetrahedronShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "DistanceFunctions.cginc"

			sampler2D _MainTex;
			uniform sampler2D _CameraDepthTexture;
			uniform float4x4 _CamFrustum, _CamToWorld;
			uniform int _MaxIterations;
			uniform float _Accuracy;
			uniform float _maxDistance, _boxround, _boxSphereSmooth, _sphereIntersectSmooth;
			uniform float4 _sphere1, _sphere2, _box1;
			uniform float3 _modInterval;
			uniform float3 _LightDir, _LightCol;
			uniform float _LightIntensity;
			uniform fixed4 _mainColor;
			uniform float2 _ShadowDistance;
			uniform float _ShadowIntensity, _ShadowPenumbra;
			uniform float _AoStepsize, _AoIntensity;
			uniform int _AoIterations;
			//uniform float3 TrianglePosition;
			uniform int _TriangleIterations;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 ray : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.ray = _CamFrustum[(int)index].xyz;
				o.ray /= abs(o.ray.z);
				o.ray = mul(_CamToWorld, float4(o.ray.xyz, 0.0));
				return o;
			}

			float BoxSphere(float3 p)
			{
				float Sphere1 = CreateSphere(p - _sphere1.xyz, _sphere1.w);
				float Box1 = CreateRoundBox(p - _box1.xyz, _box1.www, _boxround);
				float combine1 = SmoothSubtraction(Sphere1, Box1, _boxSphereSmooth);
				float Sphere2 = CreateSphere(p - _sphere2.xyz, _sphere2.w);
				float combine2 = SmoothIntersection(Sphere2, combine1, _sphereIntersectSmooth);
				return combine1;
			}

			float SierpinskiTriangle(float3 p)
			{
				float3 a1 = float3(1, 1, 1);
				float3 a2 = float3(-1, -1, 1);
				float3 a3 = float3(1, -1, -1);
				float3 a4 = float3(-1, 1, -1);
				float Scale = 2.0;
				float3 c;
				int n = 0;
				float dist, d;
				while (n < 1) {
					c = a1; 
					dist = length(p - a1);
					d = length(p - a2); 
					if (d < dist) { 
						c = a2; 
						dist = d; 
					}
					d = length(p - a3); 
					if (d < dist) { 
						c = a3; 
						dist = d; 
					}
					d = length(p - a4); 
					if (d < dist) { 
						c = a4; 
						dist = d; 
					}
					p = Scale * p - c * (Scale - 1.0);
					n++;
				}

				return length(p) * pow(Scale, float(-n));
			}

			float SierpinskiTriangle2(float3 p) 
			{
				float Scale = 2.0;
				float3 Offset = float3(1, 1, 1);
				int n = 0;
				while (n < 1) {
					if (p.x + p.y < 0) 
						p.xy = -p.yx; // fold 1
					if (p.x + p.z < 0) 
						p.xz = -p.zx; // fold 2
					if (p.y + p.z < 0) 
						p.zy = -p.yz; // fold 3	
					p = p * Scale - Offset * (Scale - 1.0);
					n++;
				}
				return (length(p)) * pow(Scale, -float(n));
			}

			float sierpinski3d(float3 p)
			{
				float scale = 2.0;
				float offset = 1.5;
				float x1, y1, z1;
				for (int n = 0; n < _TriangleIterations; n++)
				{
					p.xy = (p.x + p.y < 0.0) ? -p.yx : p.xy;
					p.xz = (p.x + p.z < 0.0) ? -p.zx : p.xz;
					p.zy = (p.z + p.y < 0.0) ? -p.yz : p.zy;

					p = scale * p - offset * (scale - 1.0);
				}

				return length(p) * pow(scale, -float(_TriangleIterations));
			}

			float sdOctahedron(float3 p, float s)
			{
				p = abs(p);
				float m = p.x + p.y + p.z - s;
				float3 q;
				if (3.0*p.x < m) q = p.xyz;
				else if (3.0*p.y < m) q = p.yzx;
				else if (3.0*p.z < m) q = p.zxy;
				else return m * 0.57735027;

				float k = clamp(0.5*(q.z - q.y + s), 0.0, s);
				return length(float3(q.x, q.y - s + k, q.z - k));
			}

			float DistanceField(float3 p)
			{
				//float boxSphere1 = BoxSphere(p);
				float test = SierpinskiTriangle2(p);

				return sierpinski3d(p);
			}

			float3 GetNormal(float3 p)
			{
				const float2 offset = float2(0.001, 0.0);
				float3 n = float3(
					DistanceField(p + offset.xyy) - DistanceField(p - offset.xyy),
					DistanceField(p + offset.yxy) - DistanceField(p - offset.yxy),
					DistanceField(p + offset.yyx) - DistanceField(p - offset.yyx));
				return normalize(n);
			}

			float HardShadow(float3 ro, float3 rd, float mint, float maxt)
			{
				for (float t = mint; t < maxt;)
				{
					float h = DistanceField(ro + rd * t);
					if (h < 0.001)
						return 0.0;
					t += h;
				}
				return 1.0;
			}

			float SoftShadow(float3 ro, float3 rd, float mint, float maxt, float k)
			{
				float result = 1.0;
				for (float t = mint; t < maxt;)
				{
					float h = DistanceField(ro + rd * t);
					if (h < 0.001)
						return 0.0;
					result = min(result, k*h / t);
					t += h;
				}
				return result;
			}

			float AmbientOcclusion(float3 p, float3 n)
			{
				float step = _AoStepsize;
				float ao = 0.0;
				float dist;
				for (int i = 1; i <= _AoIterations; i++)
				{
					dist = step * i;
					ao += max(0.0,(dist - DistanceField(p + n * dist)) / dist);
				}
				return (1.0 - ao * _AoIntensity);
			}

			float3 Shading(float3 p, float3 n)
			{
				float3 result;
				//Diffuse Color
				float3 color = _mainColor.rgb;
				//Directional Light
				float3 light = (_LightCol * dot(-_LightDir, n) * 0.5 + 0.5) * _LightIntensity;
				//Shadows
				float shadow = SoftShadow(p, -_LightDir, _ShadowDistance.x, _ShadowDistance.y,_ShadowPenumbra) * 0.5 + 0.5;
				shadow = max(0.0, pow(shadow, _ShadowIntensity));
				//Ambient Occlusion
				float ao = AmbientOcclusion(p,n);

				result = color * light * shadow * ao;

				return result;

			}

			fixed4 RayMarch(float3 ro, float3 rd, float depth)
			{
				fixed4 result = fixed4(1,1,1,1);
				const int max_iteration = _MaxIterations;
				float t = 0; //distance travelled along the ray direction

				for (int i = 0; i < max_iteration; i++)
				{
					if (t > _maxDistance || t >= depth)
					{
						//Environment
						result = fixed4(rd,0);
						break;
					}

					float3 p = ro + rd * t;
					//check for hit in DistanceField
					float d = DistanceField(p);
					if (d < _Accuracy) //We have hit something!
					{
						//shading!
						float3 n = GetNormal(p);
						//float3 s = Shading(p,n);

						result = fixed4(n,1);
						break;
					}
					t += d;
				}

				return result;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
				depth *= length(i.ray);
				fixed3 col = tex2D(_MainTex, i.uv);
				float3 rayDirection = normalize(i.ray.xyz);
				float3 rayOrigin = _WorldSpaceCameraPos;
				fixed4 result = RayMarch(rayOrigin, rayDirection, depth);
				return fixed4(col * (1.0 - result.w) + result.xyz * result.w,1.0);
			}
			ENDCG
		}
	}
}
