float _EmberTerrainEnabled;
float EmberHill(float2 p,float2 center,float radius,float height)
{float q=saturate(1-dot(p-center,p-center)/(radius*radius));return height*q*q*q;}
float EmberHeight(float2 p)
{return _EmberTerrainEnabled*(EmberHill(p,float2(-13,-11),7,1.25)+EmberHill(p,float2(16,1),5.5,.85)+EmberHill(p,float2(-17,13),5,1.05));}
