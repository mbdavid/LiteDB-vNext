namespace LiteDB.Engine;

internal class AllocationMapSession
{
    private readonly byte _colID;

    private int _currentExtendIndex = -1;
    private int _currentAllocationMapID = -1;

    private byte[] _currentExtendPagesValues = new byte[AM_EXTEND_SIZE]; // armazena a situacao das 8 paginas do extend lido

    private Dictionary<int, uint> _sessionExtends = new();

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

    public void UpdatePage(PageHeader header)
    {

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
