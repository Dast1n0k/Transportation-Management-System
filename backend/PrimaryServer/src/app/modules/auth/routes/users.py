from flask import Blueprint, request, jsonify, g
from app.modules.auth.decorators.auth import token_required, admin_required
from app.modules.auth.services.user_service import (
    get_all_users, create_user_by_admin, delete_user_by_id, change_user_role_by_id
)

users_routes = Blueprint("users_routes", __name__)


@users_routes.route("/users", methods=["GET"])
@token_required
@admin_required
def get_users():
    """Получение списка всех пользователей (только админы)"""
    users = get_all_users()

    return jsonify({
        'users': users,
        'total': len(users)
    }), 200


@users_routes.route("/users", methods=["POST"])
@token_required
@admin_required
def create_user():
    """Создание нового пользователя (только админы)"""
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    username = data.get("username", "").strip()
    password = data.get("password", "")
    role = data.get("role", "manager")

    success, error = create_user_by_admin(username, password, role)

    if error:
        status_code = 409 if error.get('code') == 'USERNAME_EXISTS' else 400
        return jsonify(error), status_code

    return jsonify({
        'message': f'User {username} created successfully as {role}',
        'created_by': g.current_user['username']
    }), 201


@users_routes.route("/users/<user_id>", methods=["DELETE"])
@token_required
@admin_required
def delete_user(user_id):
    """Удаление пользователя (только админы)"""
    current_user = g.current_user

    deleted_user, error = delete_user_by_id(user_id, current_user)

    if error:
        status_code = 404 if error.get('code') == 'USER_NOT_FOUND' else 400
        return jsonify(error), status_code

    return jsonify({
        'message': f'User {deleted_user["username"]} deleted successfully',
        'deleted_by': current_user['username']
    }), 200


@users_routes.route("/users/<user_id>/role", methods=["PUT"])
@token_required
@admin_required
def change_user_role(user_id):
    """Изменение роли пользователя (только админы)"""
    current_user = g.current_user
    data = request.get_json()

    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    new_role = data.get("role")

    updated_user, error = change_user_role_by_id(
        user_id, new_role, current_user)

    if error:
        status_code = 404 if error.get('code') == 'USER_NOT_FOUND' else 400
        return jsonify(error), status_code

    return jsonify({
        'message': f'User {updated_user["username"]} role changed to {new_role}',
        'changed_by': current_user['username']
    }), 200
