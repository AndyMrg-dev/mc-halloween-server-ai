import sqlite3
from contextlib import contextmanager
from datetime import datetime

DB_PATH = "nosferatu_data.db"

# Reputation boundaries
MIN_REP = -300
MAX_REP = 100


@contextmanager
def get_connection():
    # WAL mode prevents locks under concurrent chat threads; commit/rollback & close run automatically
    conn = sqlite3.connect(DB_PATH, timeout=10.0)
    conn.execute("PRAGMA journal_mode = WAL")
    conn.execute("PRAGMA foreign_keys = ON")
    try:
        yield conn
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def _now():
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def init_db():
    with get_connection() as conn:
        conn.execute("""
            CREATE TABLE IF NOT EXISTS players (
                player_name TEXT PRIMARY KEY,
                reputation INTEGER NOT NULL DEFAULT 0,
                deaths INTEGER NOT NULL DEFAULT 0,
                interactions INTEGER NOT NULL DEFAULT 0,
                last_seen TEXT NOT NULL
            )
        """)
        conn.execute("""
            CREATE TABLE IF NOT EXISTS action_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                player_name TEXT NOT NULL,
                message TEXT,
                ai_response TEXT,
                rep_change INTEGER NOT NULL DEFAULT 0,
                new_rep INTEGER NOT NULL DEFAULT 0,
                actions TEXT,
                created_at TEXT NOT NULL,
                FOREIGN KEY (player_name) REFERENCES players(player_name)
            )
        """)
        conn.execute("""
            CREATE TABLE IF NOT EXISTS death_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                player_name TEXT NOT NULL,
                cause TEXT,
                rep_before INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                FOREIGN KEY (player_name) REFERENCES players(player_name)
            )
        """)


def _ensure_player(conn, player):
    # Insert player if not exists, otherwise just update last_seen
    conn.execute("""
        INSERT INTO players (player_name, last_seen)
        VALUES (?, ?)
        ON CONFLICT(player_name) DO UPDATE SET last_seen = excluded.last_seen
    """, (player, _now()))


def _read_rep(conn, player):
    row = conn.execute(
        "SELECT reputation FROM players WHERE player_name = ?", (player,)
    ).fetchone()
    return row[0] if row else 0


def get_reputation(player):
    with get_connection() as conn:
        return _read_rep(conn, player)


def add_reputation(player, delta):
    with get_connection() as conn:
        _ensure_player(conn, player)
        new_rep = max(MIN_REP, min(MAX_REP, _read_rep(conn, player) + delta))
        conn.execute(
            "UPDATE players SET reputation = ? WHERE player_name = ?",
            (new_rep, player),
        )
        return new_rep


def log_interaction(player, message, ai_response, rep_change, new_rep, actions):
    now = _now()
    with get_connection() as conn:
        _ensure_player(conn, player)
        conn.execute(
            "UPDATE players SET interactions = interactions + 1, last_seen = ? WHERE player_name = ?",
            (now, player),
        )
        conn.execute("""
            INSERT INTO action_logs (player_name, message, ai_response, rep_change, new_rep, actions, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, (player, message, ai_response, rep_change, new_rep, actions, now))


def reset_reputation_on_death(player, cause):
    now = _now()
    with get_connection() as conn:
        _ensure_player(conn, player)
        rep_before = _read_rep(conn, player)

        conn.execute("""
            UPDATE players
            SET reputation = 0, deaths = deaths + 1, last_seen = ?
            WHERE player_name = ?
        """, (now, player))

        conn.execute("""
            INSERT INTO death_logs (player_name, cause, rep_before, created_at)
            VALUES (?, ?, ?, ?)
        """, (player, cause, rep_before, now))

        return rep_before