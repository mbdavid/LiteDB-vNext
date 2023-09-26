﻿//using LiteDB.Engine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using static LiteDB.Constants;

//namespace LiteDB
//{
//    /// <summary>
//    /// Class to read void, one or a collection of BsonValues. Used in SQL execution commands and query returns. Use local data source (IEnumerable[BsonDocument])
//    /// </summary>
//    public class BsonDataReader : IBsonDataReader
//    {
//        private readonly IEnumerator<BsonValue> _source = null;
//        private readonly string _collection = null;
//        private readonly bool _hasValues;

//        private BsonValue _current = null;
//        private bool _isFirst;
//        private bool _disposed = false;

//        /// <summary>
//        /// Initialize with no value
//        /// </summary>
//        internal BsonDataReader()
//        {
//            _hasValues = false;
//        }

//        /// <summary>
//        /// Initialize with a single value
//        /// </summary>
//        internal BsonDataReader(BsonValue value, string collection = null)
//        {
//            _current = value;
//            _isFirst = _hasValues = true;
//            _collection = collection;
//        }

//        /// <summary>
//        /// Initialize with an IEnumerable data source
//        /// </summary>
//        internal BsonDataReader(IEnumerable<BsonValue> values, string collection)
//        {
//            _source = values.GetEnumerator();
//            _collection = collection;

//            if (_source.MoveNext())
//            {
//                _hasValues = _isFirst = true;
//                _current = _source.Current;
//            }
//        }

//        /// <summary>
//        /// Return if has any value in result
//        /// </summary>
//        public bool HasValues => _hasValues;

//        /// <summary>
//        /// Return current value
//        /// </summary>
//        public BsonValue Current => _current;

//        /// <summary>
//        /// Return collection name
//        /// </summary>
//        public string Collection => _collection;

//        /// <summary>
//        /// Move cursor to next result. Returns true if read was possible
//        /// </summary>
//        public async ValueTask<bool> ReadAsync()
//        {
//            if (!_hasValues) return false;

//            if (_isFirst)
//            {
//                _isFirst = false;
//                return true;
//            }
//            else
//            {
//                if (_source != null)
//                {
//                    var read = _source.MoveNext();
//                    _current = _source.Current;
//                    return read;
//                }
//                else
//                {
//                    return false;
//                }
//            }
//        }
        
//        public BsonValue this[string field]
//        {
//            get
//            {
//                return _current.AsDocument[field] ?? BsonValue.Null;
//            }
//        }

//        public void Dispose()
//        {
//            this.Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        ~BsonDataReader()
//        {
//            this.Dispose(false);
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (_disposed) return;

//            _disposed = true;

//            if (disposing)
//            {
//                _source?.Dispose();
//            }
//        }
//    }
//}