float _EmberTerrainEnabled,_EmberTerrainDungeon,_EmberTerrainRadius;
float EmberHill(float2 p,float2 center,float radius,float height)
{float q=saturate(1-dot(p-center,p-center)/(radius*radius));return height*q*q*q;}
float EmberHeight(float2 p)
{
 if(_EmberTerrainDungeon>.5){float r=_EmberTerrainRadius;float side=saturate((abs(p.x)-3)/3);side=side*side*(3-2*side);float crossing=saturate((abs(p.y)-3.3)/2);crossing=crossing*crossing*(3-2*crossing);
 return _EmberTerrainEnabled*side*crossing*(EmberHill(p,float2(-r*.52,-r*.38),r*.42,2.4)+EmberHill(p,float2(r*.48,r*.42),r*.40,2.1)+EmberHill(p,float2(-r*.46,r*.50),r*.34,1.4)+EmberHill(p,float2(r*.55,-r*.40),r*.35,1.3));}
 return _EmberTerrainEnabled*(EmberHill(p,float2(-13,-11),7,2)+EmberHill(p,float2(16,1),5.5,.85)+EmberHill(p,float2(-17,13),5,1.4)+EmberHill(p,float2(-20,-2),5,1.1)+EmberHill(p,float2(-24,-18),7.5,3.1)+EmberHill(p,float2(23,13),8,2.6)+EmberHill(p,float2(-9,25),7,2.8)+EmberHill(p,float2(11,26),6.5,1.8)+EmberHill(p,float2(27,-15),5,1.9));
}
