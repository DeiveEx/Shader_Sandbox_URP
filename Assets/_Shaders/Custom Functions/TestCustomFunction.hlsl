#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

//The name of the function must the EXACTLY the same as the name of the function. The function MUST have a precision suffix (the "_float"), but it MUST NOT be in the name of the function on the custom function node. (In this case, the node will call a function named "MyFunction" instead of "MyFunction_float")
void MyFunction_float(float A, float B, out float Out1, out float Out2) {
	Out1 = 0;
	Out2 = 1;
}

#endif