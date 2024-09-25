echo "Starting database initialization"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE USER tracelens WITH PASSWORD 'tracelenspass';
  CREATE DATABASE tracelens;
  GRANT ALL PRIVILEGES ON DATABASE tracelens TO tracelens;
EOSQL

echo "Database initialization completed"