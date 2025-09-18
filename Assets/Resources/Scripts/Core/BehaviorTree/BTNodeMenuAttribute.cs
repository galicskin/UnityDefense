// Editor/ 또는 런타임 어디든 OK
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BTNodeMenuAttribute : System.Attribute
{
    public readonly string path;
    public BTNodeMenuAttribute(string path) => this.path = path;
}


