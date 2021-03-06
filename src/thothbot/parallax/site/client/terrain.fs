uniform vec3 uAmbientColor;
uniform vec3 uDiffuseColor;
uniform vec3 uSpecularColor;
uniform float uShininess;
uniform float uOpacity;

uniform bool enableDiffuse1;
uniform bool enableDiffuse2;
uniform bool enableSpecular;

uniform sampler2D tDiffuse1;
uniform sampler2D tDiffuse2;
uniform sampler2D tDetail;
uniform sampler2D tNormal;
uniform sampler2D tSpecular;
uniform sampler2D tDisplacement;

uniform float uNormalScale;

uniform vec2 uRepeatOverlay;
uniform vec2 uRepeatBase;

uniform vec2 uOffset;

varying vec3 vTangent;
varying vec3 vBinormal;
varying vec3 vNormal;
varying vec2 vUv;

uniform vec3 ambientLightColor;

#if MAX_DIR_LIGHTS > 0

	uniform vec3 directionalLightColor[ MAX_DIR_LIGHTS ];
	uniform vec3 directionalLightDirection[ MAX_DIR_LIGHTS ];

#endif

#if MAX_HEMI_LIGHTS > 0

	uniform vec3 hemisphereLightSkyColor[ MAX_HEMI_LIGHTS ];
	uniform vec3 hemisphereLightGroundColor[ MAX_HEMI_LIGHTS ];
	uniform vec3 hemisphereLightPosition[ MAX_HEMI_LIGHTS ];

#endif

#if MAX_POINT_LIGHTS > 0

	uniform vec3 pointLightColor[ MAX_POINT_LIGHTS ];
	uniform vec3 pointLightPosition[ MAX_POINT_LIGHTS ];
	uniform float pointLightDistance[ MAX_POINT_LIGHTS ];

#endif

varying vec3 vViewPosition;

[*]

void main() {

	gl_FragColor = vec4( vec3( 1.0 ), uOpacity );

	vec3 specularTex = vec3( 1.0 );

	vec2 uvOverlay = uRepeatOverlay * vUv + uOffset;
	vec2 uvBase = uRepeatBase * vUv;

	vec3 normalTex = texture2D( tDetail, uvOverlay ).xyz * 2.0 - 1.0;
	normalTex.xy *= uNormalScale;
	normalTex = normalize( normalTex );

	if( enableDiffuse1 && enableDiffuse2 ) {

		vec4 colDiffuse1 = texture2D( tDiffuse1, uvOverlay );
		vec4 colDiffuse2 = texture2D( tDiffuse2, uvOverlay );

		#ifdef GAMMA_INPUT

			colDiffuse1.xyz *= colDiffuse1.xyz;
			colDiffuse2.xyz *= colDiffuse2.xyz;

		#endif

		gl_FragColor = gl_FragColor * mix ( colDiffuse1, colDiffuse2, 1.0 - texture2D( tDisplacement, uvBase ) );

	 } else if( enableDiffuse1 ) {

		gl_FragColor = gl_FragColor * texture2D( tDiffuse1, uvOverlay );

	} else if( enableDiffuse2 ) {

		gl_FragColor = gl_FragColor * texture2D( tDiffuse2, uvOverlay );

	}

	if( enableSpecular )
		specularTex = texture2D( tSpecular, uvOverlay ).xyz;

	mat3 tsb = mat3( vTangent, vBinormal, vNormal );
	vec3 finalNormal = tsb * normalTex;

	vec3 normal = normalize( finalNormal );
	vec3 viewPosition = normalize( vViewPosition );

	// point lights

	#if MAX_POINT_LIGHTS > 0

		vec3 pointDiffuse = vec3( 0.0 );
		vec3 pointSpecular = vec3( 0.0 );

		for ( int i = 0; i < MAX_POINT_LIGHTS; i ++ ) {

			vec4 lPosition = viewMatrix * vec4( pointLightPosition[ i ], 1.0 );
			vec3 lVector = lPosition.xyz + vViewPosition.xyz;

			float lDistance = 1.0;
			if ( pointLightDistance[ i ] > 0.0 )
				lDistance = 1.0 - min( ( length( lVector ) / pointLightDistance[ i ] ), 1.0 );

			lVector = normalize( lVector );

			vec3 pointHalfVector = normalize( lVector + viewPosition );
			float pointDistance = lDistance;

			float pointDotNormalHalf = max( dot( normal, pointHalfVector ), 0.0 );
			float pointDiffuseWeight = max( dot( normal, lVector ), 0.0 );

			float pointSpecularWeight = specularTex.r * max( pow( pointDotNormalHalf, uShininess ), 0.0 );

			pointDiffuse += pointDistance * pointLightColor[ i ] * uDiffuseColor * pointDiffuseWeight;
			pointSpecular += pointDistance * pointLightColor[ i ] * uSpecularColor * pointSpecularWeight * pointDiffuseWeight;

		}

	#endif

	// directional lights

	#if MAX_DIR_LIGHTS > 0

		vec3 dirDiffuse = vec3( 0.0 );
		vec3 dirSpecular = vec3( 0.0 );

		for( int i = 0; i < MAX_DIR_LIGHTS; i++ ) {

			vec4 lDirection = viewMatrix * vec4( directionalLightDirection[ i ], 0.0 );

			vec3 dirVector = normalize( lDirection.xyz );
			vec3 dirHalfVector = normalize( dirVector + viewPosition );

			float dirDotNormalHalf = max( dot( normal, dirHalfVector ), 0.0 );
			float dirDiffuseWeight = max( dot( normal, dirVector ), 0.0 );

			float dirSpecularWeight = specularTex.r * max( pow( dirDotNormalHalf, uShininess ), 0.0 );

			dirDiffuse += directionalLightColor[ i ] * uDiffuseColor * dirDiffuseWeight;
			dirSpecular += directionalLightColor[ i ] * uSpecularColor * dirSpecularWeight * dirDiffuseWeight;

		}

	#endif

	// hemisphere lights

	#if MAX_HEMI_LIGHTS > 0

		vec3 hemiDiffuse  = vec3( 0.0 );
		vec3 hemiSpecular = vec3( 0.0 );

		for( int i = 0; i < MAX_HEMI_LIGHTS; i ++ ) {

			vec4 lPosition = viewMatrix * vec4( hemisphereLightPosition[ i ], 1.0 );
			vec3 lVector = normalize( lPosition.xyz + vViewPosition.xyz );

			// diffuse

			float dotProduct = dot( normal, lVector );
			float hemiDiffuseWeight = 0.5 * dotProduct + 0.5;

			hemiDiffuse += uDiffuseColor * mix( hemisphereLightGroundColor[ i ], hemisphereLightSkyColor[ i ], hemiDiffuseWeight );

			// specular (sky light)

			float hemiSpecularWeight = 0.0;

			vec3 hemiHalfVectorSky = normalize( lVector + viewPosition );
			float hemiDotNormalHalfSky = 0.5 * dot( normal, hemiHalfVectorSky ) + 0.5;
			hemiSpecularWeight += specularTex.r * max( pow( hemiDotNormalHalfSky, uShininess ), 0.0 );

			// specular (ground light)

			vec3 lVectorGround = normalize( -lPosition.xyz + vViewPosition.xyz );

			vec3 hemiHalfVectorGround = normalize( lVectorGround + viewPosition );
			float hemiDotNormalHalfGround = 0.5 * dot( normal, hemiHalfVectorGround ) + 0.5;
			hemiSpecularWeight += specularTex.r * max( pow( hemiDotNormalHalfGround, uShininess ), 0.0 );

			hemiSpecular += uSpecularColor * mix( hemisphereLightGroundColor[ i ], hemisphereLightSkyColor[ i ], hemiDiffuseWeight ) * hemiSpecularWeight * hemiDiffuseWeight;

		}

	#endif

	// all lights contribution summation

	vec3 totalDiffuse = vec3( 0.0 );
	vec3 totalSpecular = vec3( 0.0 );

	#if MAX_DIR_LIGHTS > 0

		totalDiffuse += dirDiffuse;
		totalSpecular += dirSpecular;

	#endif

	#if MAX_HEMI_LIGHTS > 0

		totalDiffuse += hemiDiffuse;
		totalSpecular += hemiSpecular;

	#endif

	#if MAX_POINT_LIGHTS > 0

		totalDiffuse += pointDiffuse;
		totalSpecular += pointSpecular;

	#endif

	//"gl_FragColor.xyz = gl_FragColor.xyz * ( totalDiffuse + ambientLightColor * uAmbientColor) + totalSpecular;
	gl_FragColor.xyz = gl_FragColor.xyz * ( totalDiffuse + ambientLightColor * uAmbientColor + totalSpecular );

[*]
}