SELECT 'CREATE DATABASE developer_evaluation_products'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'developer_evaluation_products')
\gexec

SELECT 'CREATE DATABASE developer_evaluation_carts'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'developer_evaluation_carts')
\gexec

SELECT 'CREATE DATABASE developer_evaluation_users'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'developer_evaluation_users')
\gexec

SELECT 'CREATE DATABASE developer_evaluation_auth'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'developer_evaluation_auth')
\gexec