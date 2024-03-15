
class Program{

    static void Main(){
        var code = File.ReadAllText("Test.ji");
        var parser = new Parser(code);
        var vm = new VM();
        foreach(var f in parser.functions){
            vm.Add(f);
        }
        Console.WriteLine(vm.GetFunction("Main")!.Invoke(vm));
    }
}