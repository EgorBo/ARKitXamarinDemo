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

	//TODO: surface RenderPathCommand::SetShaderCommand and pass these parameters:
	float scale = 0.9;
	float offset = 0.05;
	vec2 center = vec2(0.5,0.5);
	vTexCoord = (vTexCoord - center) * vec2(scale, scale) + center;

	mat4 ycbcrToRGBTransform = mat4(
		vec4(+1.0000, +1.0000, +1.0000, +0.0000),
		vec4(+0.0000, -0.3441, +1.7720, +0.0000),
		vec4(+1.4020, -0.7141, +0.0000, +0.0000),
		vec4(-0.7010, +0.5291, -0.8860, +1.0000));

	vec4 ycbcr = vec4(texture2D(sDiffMap, vec2(vTexCoord.x + offset, vTexCoord.y)).r,
					  texture2D(sNormalMap, vTexCoord).ra, 1.0);
	gl_FragColor = ycbcrToRGBTransform * ycbcr;
}