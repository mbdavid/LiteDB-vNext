namespace LiteDB.Engine;

internal class AllocationMapSession
{
    private readonly byte _colID;

    private int _startExtendIndex;
    private int _startAllocationMapID;

    private int _currentExtendIndex;
    private int _currentAllocationMapID;

    private byte[] _currentExtendPagesValues = new byte[AM_EXTEND_SIZE]; // armazena a situacao das 8 paginas do extend lido

    private Dictionary<int, byte> _sessionPages = new();

    public AllocationMapSession(byte colID)
    {
        _colID = colID;
    }

    public (int, bool) GetFreePageID(PageType type, int length)
    {
        throw new NotImplementedException();
        // Se _currentExtendIndex = -1 busca na service a primeira extend que seja desta colecao
        // ao achar uma extend, abre as 8 paginas
        // ao abrir estas 8 paginas, verificar se elas já não estão na _sessionPages. Se tiver, 
        // verifica então se as da _session servem. Não considera a pagina lida se existir na _session
        // se servir, adicione essa pagina na _sessionPages com o value atual dela
        // se não achar nenhum extend vazio (depois de dar a volta) cria um novo extend

    }

    private int GetNextExtend(PageType type, int length)
    {
        throw new NotImplementedException();
        // começa varrendo os extends a partir do _current em diante, até chegar em algum que 
        // tenha disponivel
        // retorna a extendID criada
    }

    public void UpdatePage(PageHeader header)
    {
        // adiciona/atualiza a pageID dentro do _sessionPages (atualizando o value);
        // deve ser chamado assim que cada pagina for alterada... e não no commit
        // ps: Se a pagina atualiza for "menor" que o extend atual, poderia voltar (se liberar espaço)

    }

    public void Commit()
    {
        // persiste as _sessionPages nas AMP
    }

    public void Reset()
    {
        // só limpa a _sessionPages
        // seta os _starts = _currents
    }
}
