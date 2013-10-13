//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public interface IResultsFilter
    {
        long GetLastInsertId(IDbCommand dbCmd);

        List<T> GetList<T>(IDbCommand dbCmd);

        IList GetRefList(IDbCommand dbCmd, Type refType);

        T GetSingle<T>(IDbCommand dbCmd);

        object GetRefSingle(IDbCommand dbCmd, Type refType);
        
        T GetScalar<T>(IDbCommand dbCmd);

        object GetScalar(IDbCommand dbCmd);

        long GetLongScalar(IDbCommand dbCmd);

        List<T> GetColumn<T>(IDbCommand dbCmd);

        HashSet<T> GetColumnDistinct<T>(IDbCommand dbCmd);

        Dictionary<K, V> GetDictionary<K, V>(IDbCommand dbCmd);

        Dictionary<K, List<V>> GetLookup<K, V>(IDbCommand dbCmd);

        int ExecuteSql(IDbCommand dbCmd);
    }

    public class ResultsFilter : IResultsFilter, IDisposable
    {
        public IEnumerable Results { get; set; }
        public IEnumerable RefResults { get; set; }
        public IEnumerable ColumnResults { get; set; }
        public IEnumerable ColumnDistinctResults { get; set; }
        public IDictionary DictionaryResults { get; set; }
        public IDictionary LookupResults { get; set; }
        public object SingleResult { get; set; }
        public object RefSingleResult { get; set; }
        public object ScalarResult { get; set; }
        public long LongScalarResult { get; set; }
        public long LastInsertId { get; set; }
        public int ExecuteSqlResult { get; set; }

        public Func<IDbCommand, int> ExecuteSqlFn { get; set; }
        public Func<IDbCommand, IEnumerable> ResultsFn { get; set; }
        public Func<IDbCommand, Type, IEnumerable> RefResultsFn { get; set; }
        public Func<IDbCommand, IEnumerable> ColumnResultsFn { get; set; }
        public Func<IDbCommand, IEnumerable> ColumnDistinctResultsFn { get; set; }
        public Func<IDbCommand, IDictionary> DictionaryResultsFn { get; set; }
        public Func<IDbCommand, IDictionary> LookupResultsFn { get; set; }
        public Func<IDbCommand, object> SingleResultFn { get; set; }
        public Func<IDbCommand, Type, object> RefSingleResultFn { get; set; }
        public Func<IDbCommand, object> ScalarResultFn { get; set; }
        public Func<IDbCommand, long> LongScalarResultFn { get; set; }
        public Func<IDbCommand, long> LastInsertIdFn { get; set; }

        public Action<string> SqlFilter { get; set; }
        public bool PrintSql { get; set; }

        private readonly IResultsFilter previousFilter;

        public ResultsFilter(IEnumerable results = null)
        {
            this.Results = results ?? new object[]{};

            previousFilter = OrmLiteConfig.ResultsFilter;
            OrmLiteConfig.ResultsFilter = this;
        }
        
        private void Filter(IDbCommand dbCmd)
        {
            if (SqlFilter != null)
            {
                SqlFilter(dbCmd.CommandText);
            }

            if (PrintSql)
            {
                Console.WriteLine(dbCmd.CommandText);
            }
        }

        private IEnumerable GetResults(IDbCommand dbCmd)
        {
            return ResultsFn != null ? ResultsFn(dbCmd) : Results;
        }

        private IEnumerable GetRefResults(IDbCommand dbCmd, Type refType)
        {
            return RefResultsFn != null ? RefResultsFn(dbCmd, refType) : RefResults;
        }

        private IEnumerable GetColumnResults(IDbCommand dbCmd)
        {
            return ColumnResultsFn != null ? ColumnResultsFn(dbCmd) : ColumnResults;
        }

        private IEnumerable GetColumnDistinctResults(IDbCommand dbCmd)
        {
            return ColumnDistinctResultsFn != null ? ColumnDistinctResultsFn(dbCmd) : ColumnDistinctResults;
        }

        private IDictionary GetDictionaryResults(IDbCommand dbCmd)
        {
            return DictionaryResultsFn != null ? DictionaryResultsFn(dbCmd) : DictionaryResults;
        }

        private IDictionary GetLookupResults(IDbCommand dbCmd)
        {
            return LookupResultsFn != null ? LookupResultsFn(dbCmd) : LookupResults;
        }

        private object GetSingleResult(IDbCommand dbCmd)
        {
            return SingleResultFn != null ? SingleResultFn(dbCmd) : SingleResult;
        }

        private object GetRefSingleResult(IDbCommand dbCmd, Type refType)
        {
            return RefSingleResultFn != null ? RefSingleResultFn(dbCmd, refType) : RefSingleResult;
        }

        private object GetScalarResult(IDbCommand dbCmd)
        {
            return ScalarResultFn != null ? ScalarResultFn(dbCmd) : ScalarResult;
        }

        private long GetLongScalarResult(IDbCommand dbCmd)
        {
            return LongScalarResultFn != null ? LongScalarResultFn(dbCmd) : LongScalarResult;
        }

        public long GetLastInsertId(IDbCommand dbCmd)
        {
            return LastInsertIdFn != null ? LastInsertIdFn(dbCmd) : LastInsertId;
        }

        public List<T> GetList<T>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            return (from object result in GetResults(dbCmd) select (T)result).ToList();
        }

        public IList GetRefList(IDbCommand dbCmd, Type refType)
        {
            Filter(dbCmd);
            var list = (IList)typeof(List<>).MakeGenericType(refType).CreateInstance();
            foreach (object result in GetRefResults(dbCmd, refType))
            {
                list.Add(result);
            }
            return list;
        }

        public T GetSingle<T>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            if (SingleResult != null || SingleResultFn != null)
                return (T)GetSingleResult(dbCmd);

            foreach (var result in GetResults(dbCmd))
            {
                return (T)result;
            }
            return default(T);
        }

        public object GetRefSingle(IDbCommand dbCmd, Type refType)
        {
            Filter(dbCmd);
            if (RefSingleResult != null || RefSingleResultFn != null)
                return GetRefSingleResult(dbCmd, refType);

            foreach (var result in GetRefResults(dbCmd, refType))
            {
                return result;
            }
            return null;
        }

        public T GetScalar<T>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            return ConvertTo<T>(GetScalarResult(dbCmd));
        }

        public long GetLongScalar(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            return GetLongScalarResult(dbCmd);
        }

        private T ConvertTo<T>(object value)
        {
            if (value == null)
                return default(T);

            if (value is T)
                return (T)value;

            var typeCode = typeof(T).GetUnderlyingTypeCode();
            var strValue = value.ToString();
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return (T)(object)Convert.ToBoolean(strValue);
                case TypeCode.Byte:
                    return (T)(object)Convert.ToByte(strValue);
                case TypeCode.Int16:
                    return (T)(object)Convert.ToInt16(strValue);
                case TypeCode.Int32:
                    return (T)(object)Convert.ToInt32(strValue);
                case TypeCode.Int64:
                    return (T)(object)Convert.ToInt64(strValue);
                case TypeCode.Single:
                    return (T)(object)Convert.ToSingle(strValue);
                case TypeCode.Double:
                    return (T)(object)Convert.ToDouble(strValue);
                case TypeCode.Decimal:
                    return (T)(object)Convert.ToDecimal(strValue);
            }

            return (T)value;
        }

        public object GetScalar(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            return GetScalarResult(dbCmd) ?? GetResults(dbCmd).Cast<object>().FirstOrDefault();
        }

        public List<T> GetColumn<T>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            return (from object result in GetColumnResults(dbCmd) select (T)result).ToList();
        }

        public HashSet<T> GetColumnDistinct<T>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            var results = GetColumnDistinctResults(dbCmd) ?? GetColumnResults(dbCmd);
            return (from object result in results select (T)result).ToHashSet();
        }

        public Dictionary<K, V> GetDictionary<K, V>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            var to = new Dictionary<K, V>();
            var map = GetDictionaryResults(dbCmd);
            if (map == null)
                return to;

            foreach (DictionaryEntry entry in map)
            {
                to.Add((K)entry.Key, (V)entry.Value);
            }

            return to;
        }

        public Dictionary<K, List<V>> GetLookup<K, V>(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            var to = new Dictionary<K, List<V>>();
            var map = GetLookupResults(dbCmd);
            if (map == null)
                return to;

            foreach (DictionaryEntry entry in map)
            {
                var key = (K)entry.Key;

                List<V> list;
                if (!to.TryGetValue(key, out list))
                {
                    to[key] = list = new List<V>();
                }

                list.AddRange(from object item in (IEnumerable) entry.Value select (V) item);
            }

            return to;
        }

        public int ExecuteSql(IDbCommand dbCmd)
        {
            Filter(dbCmd);
            if (ExecuteSqlFn != null)
            {
                return ExecuteSqlFn(dbCmd);
            }
            return ExecuteSqlResult;
        }

        public void Dispose()
        {
            OrmLiteConfig.ResultsFilter = previousFilter;
        }
    }
}