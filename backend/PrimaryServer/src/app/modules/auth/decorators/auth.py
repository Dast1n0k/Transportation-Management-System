import functools
from flask import jsonify, g
from app.modules.auth.utils.tokens import get_current_user


def token_required(f):
    """Декоратор для проверки JWT токена"""
    @functools.wraps(f)
    def decorated(*args, **kwargs):
        current_user = get_current_user()
        if not current_user:
            return jsonify({
                'error': 'Token is missing or invalid',
                'code': 'TOKEN_REQUIRED'
            }), 401

        g.current_user = current_user
        return f(*args, **kwargs)
    return decorated


def admin_required(f):
    @functools.wraps(f)
    def decorated(*args, **kwargs):
        if not hasattr(g, 'current_user') or g.current_user.get('role') != 'admin':
            return jsonify({
                'error': 'Admin access required',
                'code': 'ADMIN_REQUIRED'
            }), 403
        return f(*args, **kwargs)
    return decorated


def manager_or_admin_required(f):
    @functools.wraps(f)
    def decorated(*args, **kwargs):
        if not hasattr(g, 'current_user') or g.current_user.get('role') not in ['admin', 'manager']:
            return jsonify({
                'error': 'Manager or Admin access required',
                'code': 'INSUFFICIENT_PRIVILEGES'
            }), 403
        return f(*args, **kwargs)
    return decorated
