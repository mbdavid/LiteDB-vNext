using System;
using System.Collections.Generic;
using System.Text;

namespace LiteDB;

/// <summary>
/// Tenho documentacao
/// </summary>
[AutoInterface(true)]
public class TesteServico : ITesteServico
{
    /// <summary>
    /// Vou criar um serviço com descrição completa
    /// em varias linhas
    /// </summary>
    /// <param name="arroz">Descricao para arroz</param>
    /// <param name="ok">Definição de ok?</param>
    public TesteServico(int arroz, string ok)
    {
        IServicesFactory s = new ServicesFactory();




    }

    /// <summary>
    /// Vou comentar a execução
    /// </summary>
    public bool Executar(int i, string a)
    {
        return true;
    }
}
