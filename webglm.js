// Multiplies two matrices and returns the result A*B.
// The arguments A and B are arrays, representing column-major matrices.
function matMul(A, B)
{
	let C = [];
	for (let i = 0; i < 4; i++) {
		for (let j = 0; j < 4; j++) {
			let v = 0;
			for (let k = 0; k < 4; k++)
				v += A[j + 4 * k] * B[k + 4 * i];
			C.push(v);
		}
	}
	return C;
}

function matVecMul(A, v)
{
	var res = []
	for (let j = 0; j < 4; j++) {
		let s = 0;
		for (let i = 0; i < 4; i++)
			s += A[i + 4 * j] * v[i];
		res.push(s);
	}
	return res;
}

function vecAdd(a, b) {
	return [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
}

function vecScale(a, s) {
	return [a[0] * s, a[1] * s, a[2] * s];
} 

function vecNormalize(a)
{
	var len = Math.sqrt(a[0]*a[0] + a[1]*a[1] + a[2]*a[2]);
	return [a[0]/len, a[1]/len, a[2]/len];
}

function vecDot(a, b)
{
	return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}

function vecCross(a, b)
{
	return [a[1]*b[2] - a[2]*b[1], a[2]*b[0] - a[0]*b[2], a[0]*b[1] - a[1]*b[0]];
}

function matTranspose(A)
{
	return [A[0], A[4], A[8], A[12],
			A[1], A[5], A[9], A[13],
			A[2], A[6], A[10], A[14],
			A[3], A[7], A[11], A[15]];
}

function rotX(theta)
{
	var c = Math.cos(theta);
	var s = Math.sin(theta);
	return [1, 0, 0, 0,
			0, c, -s, 0,
			0, s, c, 0,
			0, 0, 0, 1];
}

function rotY(theta)
{
	var c = Math.cos(theta);
	var s = Math.sin(theta);
	return [c, 0, s, 0,
			0, 1, 0, 0,
			-s, 0, c, 0,
			0, 0, 0, 1];
}

function rotZ(theta)
{
	var c = Math.cos(theta);
	var s = Math.sin(theta);
	return [c, -s, 0, 0,
			s, c, 0, 0,
			0, 0, 1, 0,
			0, 0, 0, 1];
}

function lookAt(pos, forward, up)
{
	var z = forward;
	var x = vecCross(up, z);
	var y = vecCross(z, x); // Because forward and up might not be orthogonal
	
	var worldToCam = [
		x[0], y[0], z[0], 0,
		x[1], y[1], z[1], 0,
		x[2], y[2], z[2], 0,
		vecDot(x,pos), vecDot(y,pos), vecDot(z,pos), 1,
	];
	var camToWorld = [
		x[0], x[1], x[2], 0,
		y[0], y[1], y[2], 0,
		z[0], z[1], z[2], 0,
		pos[0], pos[1], pos[2], 1
	];
	
	return { camToWorld:camToWorld, worldToCam:worldToCam };
}