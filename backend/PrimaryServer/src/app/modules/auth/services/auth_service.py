import sqlite3
import jwt
from werkzeug.security import generate_password_hash, check_password_hash
from flask import current_app
from app.modules.auth.core.db import get_db
from app.modules.auth.utils.tokens import generate_tokens, decode_token
from app.modules.auth.utils.validators import validate_user_data, validate_password


def is_first_user():
    with get_db() as conn:
        cur = conn.execute("SELECT COUNT(*) as count FROM users")
        count = cur.fetchone()["count"]
        return count == 0


def register_user(username, password):
    validation_error = validate_user_data(username, password)
    if validation_error:
        return None, validation_error

    hashed_password = generate_password_hash(password)

    with get_db() as conn:
        role = "admin" if is_first_user() else "manager"

        try:
            conn.execute(
                "INSERT INTO users (username, password, role) VALUES (?, ?, ?)",
                (username, hashed_password, role),
            )
            conn.commit()
            return role, None
        except sqlite3.IntegrityError:
            return None, {
                'error': 'Username already exists',
                'code': 'USERNAME_EXISTS'
            }


def authenticate_user(username, password):
    validation_error = validate_user_data(username, password)
    if validation_error:
        return None, None, validation_error

    with get_db() as conn:
        cur = conn.execute(
            "SELECT * FROM users WHERE username = ?", (username,))
        user = cur.fetchone()

        if not user or not check_password_hash(user["password"], password):
            return None, None, {
                'error': 'Invalid credentials',
                'code': 'INVALID_CREDENTIALS'
            }

        user_dict = dict(user)
        access_token, refresh_token = generate_tokens(user_dict)

        return access_token, refresh_token, None


def refresh_user_token(refresh_token):
    try:
        payload = jwt.decode(
            refresh_token, current_app.config['SECRET_KEY'], algorithms=['HS256'])

        if payload.get('type') != 'refresh':
            return None, None, {
                'error': 'Invalid token type',
                'code': 'INVALID_TOKEN'
            }

        # Получаем актуальные данные пользователя
        with get_db() as conn:
            cur = conn.execute(
                "SELECT * FROM users WHERE id = ?", (payload['user_id'],))
            user = cur.fetchone()

            if not user:
                return None, None, {
                    'error': 'User not found',
                    'code': 'USER_NOT_FOUND'
                }

        user_dict = dict(user)
        new_access_token, new_refresh_token = generate_tokens(user_dict)

        return new_access_token, new_refresh_token, None

    except jwt.ExpiredSignatureError:
        return None, None, {
            'error': 'Refresh token expired',
            'code': 'TOKEN_EXPIRED'
        }
    except jwt.InvalidTokenError:
        return None, None, {
            'error': 'Invalid refresh token',
            'code': 'INVALID_TOKEN'
        }
