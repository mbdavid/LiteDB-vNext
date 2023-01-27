# NBox vNext

This is a test-only repo for new LiteDB version and features. When next version are done (a beta version), all this source will be migrated to original repo. Please, this a lab repo only! Do not use this yet.

Let's talk about features confirmed:
- Async by default
- Single file (no more separete log file... will use end-file as log file)
- New AllocationMap for map free space pages
- New smaller Bson-like format
- New Query/Fetch to works with isolated cursors
- No transaction per thread (Stateless)
- Batch operations (Insert/Update/Delete documents in differents collections at same time)
- Lower memory consume (working with `Span<byte>` everywhere + MemoryPool)
- Complete refactor in `BsonValue` and `BsonExpression` to be faster (8x) and lower memory consume (3x) 
- New Aggregation framework (aggregations will be removed from GroupBy and add in a new structure)
- Simple, easy and safe way to recovery database (when not call an `await Shutdown()`) or any data corruption
- New Extend conecpt (8 pages sequencial block) per collection - fast read block pages

- New Plugin system
	- Plguins can intercept all LiteEngine classes and methods 
	- Support multiples plugins in pipe (using proxy)

And so on...
 