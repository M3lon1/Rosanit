#ifndef SPHERECHECK_INCLUDED
#define SPHERECHECK_INCLUDED



void ComputeAlpha_float(float3 SphereCenter, float SphereRadius, float3 Position, float  FadeWidth, out float Out)
{
    float dist = distance(Position, SphereCenter);
    Out = 1.0 - saturate((dist - SphereRadius) / FadeWidth);
}




#endif //SPHERECHECK_INCLUDED