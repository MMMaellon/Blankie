// This is usually called a 'remap' in most shader code, but I work in HVAC and this
// is a 'reset' to me, so I'm sticking with what I know. ;)

float reset(float inputA, float inputB, float outputA, float outputB, float input) {
	return lerp(outputA, outputB, (input - inputA) / (inputB - inputA));
}


// Inverse Lerp
float invLerp(float inputA, float inputB, float input) {
	return (input - inputA) / (inputB - inputA);
}


