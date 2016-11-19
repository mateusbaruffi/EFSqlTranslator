using System;
using System.Linq;
using System.Linq.Expressions;
using Translation.DbObjects;

namespace Translation
{
    internal static class SqlTranslationHelper
    {
        public const string JoinKeySuffix = "_jk";

        public const string SubSelectPrefix = "sq";

        public static string GetSqlOperator(ExpressionType type)
        {
            return GetSqlOperator(GetDbOperator(type));
        }

        public static bool IsNullVal(this IDbObject obj)
        {
            var dbConst = obj as IDbConstant;
            if (dbConst == null)
                return false;

            return dbConst.Val == null;
        }

        public static bool IsAnonymouse(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.Name.StartsWith("<>") || type.Name.StartsWith("VB$");
        }

        public static bool IsGrouping(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.GenericTypeArguments.Length == 2 &&
                   type == typeof(IGrouping<,>).MakeGenericType(type.GenericTypeArguments);
        }

        /// add selectable to the select and also check if the selectable is 
        /// also required to be added on group by
        public static void AddSelection(this IDbSelect dbSelect, IDbSelectable selectable, IDbObjectFactory dbFactory)
        {
            dbSelect.Selection.Add(selectable);
            if (dbSelect.GroupBys != null && !dbSelect.GroupBys.Contains(selectable))
                dbSelect.GroupBys.Add(selectable);
        }

        /// update all joins that are related to dbRef to be left outer join
        /// this is required by method such as Select or GroupBy 
        public static void UpdateJoinType(DbReference dbRef)
        {
            var joins = dbRef.OwnerSelect.Joins.Where(j => j.To == dbRef);
            foreach(var dbJoin in joins)
            {
                dbJoin.Type = JoinType.LeftOuter;
                var relatedRefs = dbJoin.Condition.GetChildren<DbReference>(r => r != dbJoin.To);
                foreach(var relatedRef in relatedRefs)
                    UpdateJoinType(relatedRef); 
            }
        }

        public static IDbSelectable[] ProcessSelection(IDbObject dbObj, IDbObjectFactory factory)
        {
            if (dbObj is IDbList<DbKeyValue>)
            {
                var keyVals = (IDbList<DbKeyValue>)dbObj;   
                return keyVals.SelectMany(kv => ProcessSelection(kv, factory)).ToArray();
            }
            else if (dbObj is DbReference)
            {
                var dbRef = (DbReference)dbObj;
                return new [] { factory.BuildRefColumn(dbRef) };
            }
            else if (dbObj is DbKeyValue)
            {
                var kv = (DbKeyValue)dbObj;
                var dbRef = kv.Value as DbReference;
                
                var selectables = ProcessSelection(kv.Value, factory);
                
                foreach(var selectable in selectables)
                    selectable.Alias = kv.Key;

                return selectables;
            }
            else
            {
                return new [] { (IDbSelectable)dbObj };
            }
        }

        public static IDbSelectable GetOrCreateSelectable(
            IDbSelectable selectable, DbReference dbRef, IDbObjectFactory dbFactory)
        {
            if (dbRef == null)
                return selectable;

            IDbSelectable newSelectable = null;
            if (selectable is IDbColumn)
            {
                var oCol = (IDbColumn)selectable;
                newSelectable = dbFactory.BuildColumn(oCol);
                newSelectable.Ref = dbRef;

                if (dbRef.Referee is IDbSelect 
                    && oCol.Ref.OwnerSelect != null 
                    && !oCol.Ref.OwnerSelect.Selection.Contains(oCol))
                {
                    oCol.Alias = null;
                    oCol.Ref.OwnerSelect.AddSelection(oCol, dbFactory);
                }
            }
            else if (selectable is IDbRefColumn)
            {
                var oRefCol = (IDbRefColumn)selectable;
                var oSelect = oRefCol.Ref.OwnerSelect;

                if (!oSelect.Selection.Contains(oRefCol))
                    oSelect.AddSelection(oRefCol, dbFactory);
                
                newSelectable = dbFactory.BuildRefColumn(dbRef, oRefCol.Alias, oRefCol);
            }
            else if (selectable is DbReference)
            {
                var oRef = (DbReference)selectable;
                newSelectable = dbFactory.BuildRefColumn(dbRef, oRef.Alias);
            }
            
            if (newSelectable == null)
                throw new InvalidOperationException();

            return newSelectable;
        }

        public static void AddRefSelection(
            this IDbRefColumn refColumn, string columnName, Type columnType, 
            IDbObjectFactory dbFactory, string alias, bool isJoinKey)
        {
            if (refColumn.RefTo != null)
            {
                refColumn.RefTo.AddRefSelection(columnName, columnType, dbFactory, alias, isJoinKey);
                columnName = alias ?? columnName;
                alias = null;
            }   

            var refToCol = dbFactory.BuildColumn(refColumn.Ref, columnName, columnType, alias);
            refToCol.IsJoinKey = isJoinKey;
            refColumn.Ref.RefSelection[columnName] = refToCol;
        }

        public static DbOperator GetDbOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.AndAlso:
                    return DbOperator.And;
                case ExpressionType.OrElse:
                    return DbOperator.Or;
                case ExpressionType.Add:
                    return DbOperator.Add;
                case ExpressionType.Subtract:
                    return DbOperator.Subtract;
                case ExpressionType.Multiply:
                    return DbOperator.Multiply;
                case ExpressionType.Divide:
                    return DbOperator.Divide;
                case ExpressionType.Equal:
                    return DbOperator.Equal;
                case ExpressionType.NotEqual:
                    return DbOperator.NotEqual;
                case ExpressionType.Not:
                    return DbOperator.Not;
                case ExpressionType.GreaterThan:
                    return DbOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return DbOperator.GreaterThan;
                case ExpressionType.LessThan:
                    return DbOperator.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return DbOperator.LessThanOrEqual;
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        public static string GetSqlOperator(DbOperator optr)
        {
            switch (optr)
            {
                case DbOperator.And:
                    return "and";
                case DbOperator.Or:
                    return "or";
                case DbOperator.Add:
                    return "+";
                case DbOperator.Subtract:
                    return "-";
                case DbOperator.Multiply:
                    return "*";
                case DbOperator.Divide:
                    return "/";
                case DbOperator.Is:
                    return "is";
                case DbOperator.IsNot:
                    return "is not";
                case DbOperator.Equal:
                    return "=";
                case DbOperator.NotEqual:
                    return "!=";
                case DbOperator.Not:
                    return "not";
                case DbOperator.GreaterThan:
                    return ">";
                case DbOperator.GreaterThanOrEqual:
                    return ">=";
                case DbOperator.LessThan:
                    return "<";
                case DbOperator.LessThanOrEqual:
                    return "<=";
                default:
                    throw new NotSupportedException(optr.ToString());
            }
        }
    }
}