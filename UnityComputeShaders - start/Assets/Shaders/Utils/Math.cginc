#define PI 3.14159265359
#define PI2 6.28318530718
#define EPSILON 1E-5

float invLerp(float a, float b, float v)
{
    return saturate((v - a) / (b - a));
}

float4x4 look_at_matrix(float3 dir, float3 up)
{
    float3 zaxis = normalize(dir);
    float3 xaxis = normalize(cross(up, zaxis));
    float3 yaxis = cross(zaxis, xaxis);
    return float4x4(
                xaxis.x, yaxis.x, zaxis.x, 0,
                xaxis.y, yaxis.y, zaxis.y, 0,
                xaxis.z, yaxis.z, zaxis.z, 0,
                0, 0, 0, 1
            );
}

float4x4 create_matrix(float3 pos, float3 dir, float3 up)
{
    float3 zaxis = normalize(dir);
    float3 xaxis = normalize(cross(up, zaxis));
    float3 yaxis = cross(zaxis, xaxis);
    return float4x4(
                xaxis.x, yaxis.x, zaxis.x, pos.x,
                xaxis.y, yaxis.y, zaxis.y, pos.y,
                xaxis.z, yaxis.z, zaxis.z, pos.z,
                0, 0, 0, 1
            );
}

float4x4 create_matrix_xy(float3 pos, float theta)
{
    float c = cos(theta);
    float s = sin(theta);
    return float4x4(
                c, -s, 0, pos.x,
                s, c, 0, pos.y,
                0, 0, 1, pos.z,
                0, 0, 0, 1
            );
}

float4x4 create_matrix_yz(float3 pos, float theta)
{
    float c = cos(theta);
    float s = sin(theta);
    return float4x4(
                1, 0, 0, pos.x,
                0, c, -s, pos.y,
                0, s, c, pos.z,
                0, 0, 0, 1
            );
}

float4x4 quaternion_to_matrix(float4 quat, float3 pos)
{
    float4x4 m = float4x4(float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0));

    float x = quat.x, y = quat.y, z = quat.z, w = quat.w;
    float x2 = x + x, y2 = y + y, z2 = z + z;
    float xx = x * x2, xy = x * y2, xz = x * z2;
    float yy = y * y2, yz = y * z2, zz = z * z2;
    float wx = w * x2, wy = w * y2, wz = w * z2;

    m[0][0] = 1.0 - (yy + zz);
    m[0][1] = xy - wz;
    m[0][2] = xz + wy;

    m[1][0] = xy + wz;
    m[1][1] = 1.0 - (xx + zz);
    m[1][2] = yz - wx;

    m[2][0] = xz - wy;
    m[2][1] = yz + wx;
    m[2][2] = 1.0 - (xx + yy);

    m[0][3] = pos.x;
    m[1][3] = pos.y;
    m[2][3] = pos.z;
    m[3][3] = 1.0;

    return m;
}

//float4x4 create_matrix(float3 pos, float4 quat)
//{
//    float4x4 rotation = quaternion_to_matrix(quat, 0);
//    float3 position = pos;
//    float4x4 translation =
//    {
//      1, 0, 0, position.x,
//	    0, 1, 0, position.y,
//	    0, 0, 1, position.z,
//	    0, 0, 0, 1
//    };
//    return mul(translation, rotation);
//}

float4 quaternion_from_to(float3 v1, float3 v2)
{
    v1 = normalize(v1);
    v2 = normalize(v2);
    float3 v = v1 + v2;
    v = normalize(v);
    float4 q = 0;
    q.w = dot(v, v2);
    q.xyz = cross(v, v2);
    return q;
}

inline float4 quat_concat(float4 q1, float4 q2)
{
    return float4(q1.w * q2.xyz + q2.w * q1.xyz + cross(q1.xyz, q2.xyz), q1.w * q2.w - dot(q1.xyz, q2.xyz));
}

inline float3 quat_mul(float4 q, float3 v)
{
    float3 u = q.xyz;
    float s = q.w;
    //return dot(q.xyz, v) * q.xyz + q.w * q.w * v + 2.0 * q.w * cross(q.xyz, v) - cross(cross(q.xyz, v), q.xyz);
    //return dot(u, v) * u + s * s * v + 2.0 * s * cross(u, v) - cross(cross(u, v), u);
    return 2.0 * dot(u, v) * u + (s * s - dot(u, u)) * v + 2.0 * s * cross(u, v);
}
