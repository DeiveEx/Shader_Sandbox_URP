using UnityEngine;
using UnityEditor.ShaderGraph; //Import these
using System.Reflection;

//Creates the Menu. Each string parameter is a Category in the "Create Node" menu.
[Title("_Custom", "Custom Node Test", "Test")]
public class MyCustomNode : CodeFunctionNode { //Extend from this

	//In the constructor, we can define the name of the node itself.
	public MyCustomNode() {
		name = "Test Node Title";
	}

	//We have to implement this method to call our Method containing the sahder code. Change the "MyCustomFunction" string to the Method's name.
	protected override MethodInfo GetFunctionToConvert() {
		return GetType().GetMethod("MyCustomFunction", BindingFlags.Static | BindingFlags.NonPublic);
	}

	//And this is our custom Method itself. It HAS to be static and have a string return type, and the parameters are the slots of the node. We gotta use the attribute "Slot" to create the slots and using the keyword "out" makes that slot an output slot. Also, each slot HAS to have an unique ID.
	static string MyCustomFunction([Slot(0, Binding.None)] out Vector1 Out1, [Slot(1, Binding.None)] out Vector1 Out2) {
		//And this is the Shader code we want our node to execute. For some reason they made that we gotta write it as a string, so we can't use Visual Studio to check for erros and stuff. People were complaining about this, but since I don'k know Shader code, it's indifferent form me :v
		return @"
{
	Out1 = 1;
	Out2 = 2;
}";
	}
}
