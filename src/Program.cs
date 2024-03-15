
class Program{

    static void Main(){
        var code = File.ReadAllText("Test.ji");
        var parser = new Parser(code);
        Console.WriteLine(parser.GetFunction("Main")!.Invoke(parser));
    }
}