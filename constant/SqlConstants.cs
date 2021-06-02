using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocFwIpApp.constant
{
    public sealed class SqlConstants
    {
        public const String SELECT_ALL = "SELECT * FROM {0}";

        public const String SELECT_COUNT_ALL = "SELECT count(*) FROM {0}";

        public const String SELECT_ALL_WHERE = "SELECT * FROM {0} WHERE {1}";

        public const String SELECT_COUNT_ALL_WHERE = "SELECT count(*) FROM {0} WHERE {1}";

        public const String SELECT_COL_WHERE = "SELECT {0} FROM {1} WHERE {2}";

        public const String SELECT_COL_ORDERBY = "SELECT {0} FROM {1} ORDER BY {2}";

        public const String SELECT_COL_WHERE_ORDERBY = "SELECT {0} FROM {1} WHERE {2} ORDER BY {3}";


        public const String UPDATE_WHERE = "UPDATE {0} SET {1} WHERE {2}";

        public const String INSERT_INTO = "INSERT INTO {0} ({1}) values ({2})";

        public const String DELETE_WHERE = "DELETE FROM {0} WHERE {1}";
    }
}
