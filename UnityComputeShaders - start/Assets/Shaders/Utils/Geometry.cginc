#define PI 3.14159265359
#define PI2 6.28318530718

//Return 1 if pt is in the rect parameter and 0 otherwise
float inRect(float2 pt, float4 rect)
{
    float horz = step(rect.x, pt.x) - step(rect.x + rect.z, pt.x);
    float verz = step(rect.y, pt.y) - step(rect.y + rect.w, pt.y);
    return horz * verz;
}

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

float4 lerpCircleColor(float4 outColor, float4 inColor, float2 pt, float2 center, float radius, float edgeWidth)
{
    float t = inCircle(pt, center, radius, edgeWidth);
    return lerp(outColor, inColor, t);
}

void lerpColors(RWTexture2D<float4> tex, uint2 xy, float4 color, float lerpValue)
{
    tex[xy] = lerp(tex[xy], color, lerpValue);
}

void drawLine(RWTexture2D<float4> tex, int2 p1, int2 p2, float4 color, float lerpValue = 1.0)
{
    int2 absDr = abs(p2 - p1);
    
    if (absDr.x * absDr.y <= 1)
    {
        tex[p1] = lerp(tex[p1], color, lerpValue);
        tex[p2] = lerp(tex[p2], color, lerpValue);
    }
    else if (absDr.y > absDr.x)
    {
        if (p1.y > p2.y)
        {
            int2 temp = p1;
            p1 = p2;
            p2 = temp;
        }
        for (int y = p1.y; y <= p2.y; y++)
        {
            int x = (int)(lerp(p1.x, p2.x, (float)(y - p1.y) / absDr.y) + 0.5);
            lerpColors(tex, uint2(x, y), color, lerpValue);
        }
    }
    else
    {
        if (absDr.x == 0)
        {
            if (p1.y > p2.y)
            {
                int2 temp = p1;
                p1 = p2;
                p2 = temp;
            }
            int x = p1.x;
            for (int y = p1.y; y <= p2.y; y++)
            {
                lerpColors(tex, uint2(x, y), color, lerpValue);
            }
        }
        else
        {
            if (p1.x > p2.x)
            {
                int2 temp = p1;
                p1 = p2;
                p2 = temp;
            }
            for (int x = p1.x; x <= p2.x; x++)
            {
                int y = (int)(lerp(p1.y, p2.y, (float)(x - p1.x) / absDr.x) + 0.5);
                lerpColors(tex, uint2(x, y), color, lerpValue);
            }
        }
    }
}
