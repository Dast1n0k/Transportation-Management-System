import datetime
from flask import Blueprint, request, jsonify, g
from app.modules.auth.decorators.auth import token_required, admin_required, manager_or_admin_required
from app.modules.auth.services.user_service import update_user_password

profile_routes = Blueprint("profile_routes", __name__)


@profile_routes.route("/profile", methods=["GET"])
# @token_required
def get_profile():
    user = g.current_user
    return jsonify({
        'user': {
            'id': user['id'],
            'username': user['username'],
            'role': user['role']
        }
    }), 200


@profile_routes.route("/profile", methods=["PUT"])
# @token_required
def update_profile():
    current_user = g.current_user
    data = request.get_json()

    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    new_password = data.get('password')

    if new_password:
        success, error = update_user_password(current_user['id'], new_password)

        if error:
            return jsonify(error), 400

        return jsonify({
            'message': 'Password updated successfully'
        }), 200

    return jsonify({
        'error': 'No valid fields to update',
        'code': 'VALIDATION_ERROR'
    }), 400


@profile_routes.route("/admin-only", methods=["GET"])
# @token_required
# @admin_required
def admin_only():
    user = g.current_user
    return jsonify({
        'message': f'Admin access granted for {user["username"]}',
        'timestamp': datetime.datetime.utcnow().isoformat()
    }), 200


@profile_routes.route("/manager-or-admin", methods=["GET"])
# @token_required
# @manager_or_admin_required
def manager_or_admin():
    user = g.current_user
    return jsonify({
        'message': f'Manager/Admin access granted for {user["username"]}',
        'role': user['role'],
        'timestamp': datetime.datetime.utcnow().isoformat()
    }), 200
