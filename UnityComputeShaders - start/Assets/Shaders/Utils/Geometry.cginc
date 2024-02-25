#define PI 3.14159265359
#define PI2 6.28318530718

float inCircle(float2 pt, float radius)
{
    return 1.0 - step(radius, length(pt));
}

float inCircle(float2 pt, float2 center, float radius, float edgeWidth)
{
    float len = length(pt - center);
    return 1.0 - smoothstep(radius - edgeWidth, radius, len);
}

float polygon(float2 pt, float2 center, float radius, int sides, float rotate, float edge_thickness)
{
    pt -= center;

    // Angle and radius from the current pixel
    float theta = atan2(pt.y, pt.x) + rotate;
    float rad = PI2 / float(sides);

    // Shaping function that modulate the distance
    //float d = cos(fmod(PI2 + theta, rad) - 0.5 * rad) * length(pt);
    float d = cos(floor(0.5 + theta / rad) * rad - theta) * length(pt);

    return 1.0 - smoothstep(radius, radius + edge_thickness, d);
}
