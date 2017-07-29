#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"

varying highp vec2 vScreenPos;
uniform float cCameraScale;

void VS()
{
	mat4 modelMatrix = iModelMatrix;
	vec3 worldPos = GetWorldPos(modelMatrix);
	gl_Position = GetClipPos(worldPos);
	vScreenPos = GetScreenPosPreDiv(gl_Position);
}

void PS()
{ 
	//flip vertically
	vec2 vTexCoord = vec2(vScreenPos.x, 1.0 - vScreenPos.y);

	//scale up a bit
	vec2 scaleVector = vec2(0.9, 0.9);
	float offset = (1.0 - scaleVector.x) / 2.0;
	vec2 fromCenter = vTexCoord-vec2(.5,.5);
	vec2 scaledFromCenter = fromCenter*scaleVector;
	vTexCoord = vec2(.5,.5) + scaledFromCenter;

	mat4 ycbcrToRGBTransform = mat4(
		vec4(+1.0000, +1.0000, +1.0000, +0.0000),
		vec4(+0.0000, -0.3441, +1.7720, +0.0000),
		vec4(+1.4020, -0.7141, +0.0000, +0.0000),
		vec4(-0.7010, +0.5291, -0.8860, +1.0000));

	vec4 ycbcr = vec4(texture2D(sDiffMap, vec2(vTexCoord.x + offset, vTexCoord.y)).r,
					  texture2D(sNormalMap, vTexCoord).ra, 1.0);
	gl_FragColor = ycbcrToRGBTransform * ycbcr;
}