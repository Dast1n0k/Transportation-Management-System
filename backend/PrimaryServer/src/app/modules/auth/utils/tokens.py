import datetime
import jwt
from flask import request, current_app
from app.modules.auth.core.db import get_db


def extract_token():
    """Извлекает JWT токен из заголовка Authorization"""
    auth_header = request.headers.get('Authorization')
    if not auth_header:
        return None

    try:
        return auth_header.split(' ')[1]
    except IndexError:
        return None


def decode_token(token):
    """Декодирует JWT токен и возвращает payload"""
    try:
        payload = jwt.decode(
            token, current_app.config['SECRET_KEY'], algorithms=['HS256'])
        return payload
    except jwt.ExpiredSignatureError:
        return None
    except jwt.InvalidTokenError:
        return None


def get_current_user():
    """Получает текущего пользователя из токена"""
    token = extract_token()
    if not token:
        return None

    payload = decode_token(token)
    if not payload:
        return None

    # Проверяем, что пользователь еще существует в БД
    with get_db() as conn:
        cur = conn.execute("SELECT * FROM users WHERE id = ?",
                           (payload['user_id'],))
        user = cur.fetchone()
        if user:
            return dict(user)
    return None


def generate_tokens(user):
    """Генерирует access и refresh токены для пользователя"""
    now = datetime.datetime.utcnow()

    # Access token (короткое время жизни)
    access_payload = {
        'user_id': user['id'],
        'username': user['username'],
        'role': user['role'],
        'type': 'access',
        'iat': now,
        'exp': now + datetime.timedelta(hours=2)  # 2 часа
    }

    # Refresh token (длинное время жизни)
    refresh_payload = {
        'user_id': user['id'],
        'type': 'refresh',
        'iat': now,
        'exp': now + datetime.timedelta(days=30)  # 30 дней
    }

    access_token = jwt.encode(
        access_payload, current_app.config['SECRET_KEY'], algorithm='HS256')
    refresh_token = jwt.encode(
        refresh_payload, current_app.config['SECRET_KEY'], algorithm='HS256')

    return access_token, refresh_token
