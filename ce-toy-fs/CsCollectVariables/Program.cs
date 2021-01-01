using System;

namespace CsCollectVariables
{
  interface IA {}
  interface IB {}

  class Program
  {
    void A(IA env) {}
    void B(IB env) {}
    void C<T>(T env) 
      where T : IA, IB
    {
      A(env);
      B(env);
    }
    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");
    }
  }
}
