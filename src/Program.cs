
class Program{

    static void Main(){
        var code = File.ReadAllText("Test.ji");
        var functions = Parser.ParseFunctions(code);
        var vm = new VM();
        foreach(var f in functions){
            vm.Add(f);
        }
        Console.WriteLine(vm.GetFunction("Main")!.Invoke(vm));
    }
}