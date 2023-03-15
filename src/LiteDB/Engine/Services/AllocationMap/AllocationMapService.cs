using System;
using System.Threading;

namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class AllocationMapService : IAllocationMapService
{
    private readonly IServicesFactory _factory;
    private List<AllocationMapPage> _pages = new();

    /// <summary>
    /// A struct, per colID, to store a list of pages with available space
    /// </summary>
    private readonly CollectionFreePages[] _collectionFreePages = new CollectionFreePages[byte.MaxValue];

    public AllocationMapService(IServicesFactory factory)
    {
        _factory = factory;
    }

    public async Task Initialize()
    {
        var disk = _factory.Disk;

        // read all allocation maps pages on disk
        await foreach (var pageBuffer in disk.ReadAllocationMapPages())
        {
            // get page buffer from disk
            var page = new AllocationMapPage(pageBuffer);

            // read all collection map in memory
            page.ReadAllocationMap(_collectionFreePages);

            // add amp to instance
            _pages.Add(page);
        }
    }

    /// <summary>
    /// Return a page ID with space avaiable to store length bytes. Support only DataPages and IndexPages.
    /// Return pageID and bool to indicate that this page is a new empty page (must be created)
    /// </summary>  
    public (uint, bool) GetFreePageID(byte colID, PageType type, int length)
    {
        //TODO: cassiano, posso retornar uma pagina com tamanho menor do solicitado?
        // o chamador que peça uma nova com o restante (while)
        // ta feio assim, mas tem como ficar bonito e eficiente (sem alocar memoria)

        var freePages = _collectionFreePages[colID];

        if (type == PageType.Data)
        {
            // test if length for SMALL size document length
            if (length < AMP_DATA_PAGE_SPACE_3)
            {
                if (freePages.DataPages_3.Count > 0)
                {
                    return (freePages.DataPages_3.Dequeue(), false);
                }
                else if (freePages.DataPages_2.Count > 0)
                {
                    return (freePages.DataPages_2.Dequeue(), false);
                }
                else if (freePages.DataPages_1.Count > 0)
                {
                    return (freePages.DataPages_1.Dequeue(), false);
                }
                else if (freePages.EmptyPages.Count > 0)
                {
                    return (freePages.EmptyPages.Dequeue(), true);
                }
                else
                {
                    // deve criar uma nova extend ou mesmo pesquisar em outra amp
                    // pode ser que chame novamente a mesma função
                    throw new NotImplementedException();
                }
            }

            // test if length for MIDDLE size document length
            else if (length < AMP_DATA_PAGE_SPACE_2)
            {
                if (freePages.DataPages_1.Count > 0)
                {
                    return (freePages.DataPages_1.Dequeue(), false);
                }
                else if (freePages.EmptyPages.Count > 0)
                {
                    return (freePages.EmptyPages.Dequeue(), true);
                }
                else
                {
                    // deve criar uma nova extend ou mesmo pesquisar em outra amp
                    // pode ser que chame novamente a mesma função
                    throw new NotImplementedException();
                }
            }

            // test if length for LARGE size document length (considering 1 page block)
            else if (length < AMP_DATA_PAGE_SPACE_1)
            {
                if (freePages.DataPages_1.Count > 0)
                {
                    return (freePages.DataPages_1.Dequeue(), false);
                }
                else if (freePages.EmptyPages.Count > 0)
                {
                    return (freePages.EmptyPages.Dequeue(), true);
                }
                else
                {
                    // deve criar uma nova extend ou mesmo pesquisar em outra amp
                    // pode ser que chame novamente a mesma função
                    throw new NotImplementedException();
                }
            }

            else  // length >= AMP_DATA_PAGE_SPACE_2
            {
                if (freePages.DataPages_1.Count > 0)
                {
                    return (freePages.DataPages_1.Dequeue(), false);
                }
                else if (freePages.EmptyPages.Count > 0)
                {
                    return (freePages.EmptyPages.Dequeue(), true);
                }
                else
                {
                    // deve criar uma nova extend ou mesmo pesquisar em outra amp
                    // pode ser que chame novamente a mesma função
                    throw new NotImplementedException();
                }
            }
        }
        else // PageType = IndexPage
        {
            if (freePages.IndexPages.Count > 0)
            {
                return (freePages.IndexPages.Dequeue(), false);
            }
            else if (freePages.EmptyPages.Count > 0)
            {
                return (freePages.EmptyPages.Dequeue(), true);
            }
            else
            {
                // deve criar uma nova extend ou mesmo pesquisar em outra amp
                // pode ser que chame novamente a mesma função
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Create a new extend in any allocation map page that contains space avaiable. If all pages are full, create another allocation map page
    /// Return the first empty pageID created for this collection in this new extend
    /// This method populate collectionFreePages[colID] with 8 new empty pages
    /// </summary>
    private uint CreateNewExtend(byte colID)
    {
        // lock, pois não pode ter 2 threads aqui

        return 0;
    }

    public void Dispose()
    {
        // limpar paginas
    }
}