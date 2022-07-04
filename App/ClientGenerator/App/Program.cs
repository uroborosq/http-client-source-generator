namespace App;

public class Program
{
    public static void Main(string[] args)
    {
        ClientGen.GenerateClient(
            "/home/uroborosq/Рабочий стол/Технологии программирования/lab-2/PythonHttpServer/models.py",
            "/home/uroborosq/Рабочий стол/Технологии программирования/lab-2/PythonHttpServer/main.py",
            "http://localhost:8000"
            );
    }
}

