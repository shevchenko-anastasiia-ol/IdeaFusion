from app.config import to_libpq


def test_npgsql_connection_string_is_converted():
    conn = "Host=localhost;Port=5432;Username=postgres;Password=p@ss;Database=imagecopydb"
    assert to_libpq(conn) == "host='localhost' port='5432' user='postgres' password='p@ss' dbname='imagecopydb'"


def test_urls_and_conninfo_pass_through():
    assert to_libpq("postgresql://u:p@h/db") == "postgresql://u:p@h/db"
    assert to_libpq("host=h dbname=db") == "host=h dbname=db"
