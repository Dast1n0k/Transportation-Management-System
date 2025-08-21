from app.modules.auth.core.db import get_db
import datetime
from werkzeug.security import check_password_hash
from flask import Blueprint, request, jsonify, g
from app.modules.auth.services.auth_service import register_user

auth_routes = Blueprint("auth_routes", __name__, url_prefix="/auth")


@auth_routes.route("/register", methods=["POST"])
def register():
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    username = data.get("username", "").strip()
    password = data.get("password", "")

    role, error = register_user(username, password)

    if error:
        status_code = 409 if error.get('code') == 'USERNAME_EXISTS' else 400
        return jsonify(error), status_code

    return jsonify({
        'message': f'User {username} registered successfully as {role}',
        'role': role
    }), 201


@auth_routes.route("/login", methods=["POST"])
def login():
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    username = data.get("username", "").strip()
    password = data.get("password", "")

    if not username or not password:
        return jsonify({'error': 'Username and password required'}), 400

    with get_db() as conn:
        cur = conn.execute(
            "SELECT * FROM users WHERE username = ?", (username,)
        )
        user = cur.fetchone()

    if not user or not check_password_hash(user["password"], password):
        return jsonify({
            'error': 'Invalid credentials',
            'code': 'INVALID_CREDENTIALS'
        }), 401

    return jsonify({
        'message': 'Login successful',
        'user': {
            'id': user['id'],
            'username': user['username'],
            'role': user['role']
        }
    }), 200


# @auth_routes.route("/refresh", methods=["POST"])
# def refresh_token():
#     data = request.get_json()
#     if not data:
#         return jsonify({'error': 'JSON data required'}), 400

#     refresh_token = data.get('refresh_token')
#     if not refresh_token:
#         return jsonify({
#             'error': 'Refresh token is required',
#             'code': 'TOKEN_REQUIRED'
#         }), 400

#     new_access_token, new_refresh_token, error = refresh_user_token(
#         refresh_token)

#     if error:
#         status_code = 404 if error.get('code') == 'USER_NOT_FOUND' else 401
#         return jsonify(error), status_code

#     return jsonify({
#         'access_token': new_access_token,
#         'refresh_token': new_refresh_token
#     }), 200


@auth_routes.route("/verify", methods=["GET"])
# @token_required
def verify_token():
    user = g.current_user
    return jsonify({
        'valid': True,
        'user': {
            'id': user['id'],
            'username': user['username'],
            'role': user['role']
        }
    }), 200


@auth_routes.route("/logout", methods=["POST"])
# @token_required
def logout():
    """Выход из системы"""
    return jsonify({
        'message': 'Logged out successfully'
    }), 200


@auth_routes.route("/protected", methods=["GET"])
# @token_required
def protected():
    user = g.current_user
    return jsonify({
        'message': f'Hello {user["username"]}! Your role is {user["role"]}.',
        'timestamp': datetime.datetime.utcnow().isoformat()
    }), 200
