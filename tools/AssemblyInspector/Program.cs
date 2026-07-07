using System.Reflection;

if (args.Length < 2) {
    Console.Error.WriteLine("Usage: AssemblyInspector <managed-dir> <type-name> [...]");
    return 2;
}

string managedDirectory = Path.GetFullPath(args[0]);
var paths = Directory.GetFiles(managedDirectory, "*.dll");
var resolver = new PathAssemblyResolver(paths);
using var context = new MetadataLoadContext(resolver, "mscorlib");

Assembly assembly = context.LoadFromAssemblyPath(
    Path.Combine(managedDirectory, "Assembly-CSharp.dll"));
Assembly firstPass = context.LoadFromAssemblyPath(
    Path.Combine(managedDirectory, "Assembly-CSharp-firstpass.dll"));
Assembly[] assemblies = { assembly, firstPass };
Type[] allTypes = assemblies.SelectMany(item => item.GetTypes()).ToArray();

foreach (string query in args.Skip(1)) {
    Console.WriteLine("===== " + query + " =====");
    if (query.StartsWith("derived:", StringComparison.OrdinalIgnoreCase)) {
        string baseName = query.Substring("derived:".Length);
        Type? baseType = allTypes.FirstOrDefault(type =>
            string.Equals(type.Name, baseName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type.FullName, baseName, StringComparison.OrdinalIgnoreCase));
        if (baseType == null) {
            Console.WriteLine("  (base type not found)");
            continue;
        }
        foreach (Type type in allTypes.Where(type => type != baseType &&
                IsDerivedFrom(type, baseType)).OrderBy(type => type.FullName))
            Console.WriteLine("  " + type.FullName);
        continue;
    }
    bool found = false;
    foreach (Assembly candidateAssembly in assemblies) {
        foreach (Type type in candidateAssembly.GetTypes().Where(type =>
                string.Equals(type.Name, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.FullName, query, StringComparison.OrdinalIgnoreCase))) {
            found = true;
            Console.WriteLine(type.FullName + " @ " + candidateAssembly.GetName().Name);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (FieldInfo field in type.GetFields(flags).OrderBy(item => item.Name))
                Console.WriteLine("  FIELD " + FriendlyName(field.FieldType) + " " +
                    field.Name);
            foreach (PropertyInfo property in type.GetProperties(flags)
                    .OrderBy(item => item.Name))
                Console.WriteLine("  PROPERTY " + FriendlyName(property.PropertyType) +
                    " " + property.Name);
            foreach (MethodInfo method in type.GetMethods(flags).OrderBy(item => item.Name)) {
                string parameters = string.Join(", ", method.GetParameters().Select(item =>
                    FriendlyName(item.ParameterType) + " " + item.Name));
                Console.WriteLine("  " + method.Attributes + " " +
                    FriendlyName(method.ReturnType) + " " + method.Name + "(" +
                    parameters + ")");
            }
        }
    }
    if (!found)
        Console.WriteLine("  (not found)");
}

return 0;

static string FriendlyName(Type type) {
    if (!type.IsGenericType)
        return type.FullName ?? type.Name;
    string name = type.GetGenericTypeDefinition().FullName ?? type.Name;
    int tick = name.IndexOf('`');
    if (tick >= 0)
        name = name.Substring(0, tick);
    return name + "<" + string.Join(", ", type.GetGenericArguments().Select(FriendlyName)) +
        ">";
}

static bool IsDerivedFrom(Type type, Type expectedBase) {
    for (Type? current = type.BaseType; current != null; current = current.BaseType) {
        if (current == expectedBase)
            return true;
    }
    return false;
}
