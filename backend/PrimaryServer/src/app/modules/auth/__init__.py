from flask import Blueprint
from .routes.auth import auth_routes
from .routes.users import users_routes
from .routes.profile import profile_routes


def create_auth_blueprint():
    """Создает и настраивает blueprint для auth модуля"""
    auth_bp = Blueprint("auth", __name__, url_prefix="/auth")

    auth_bp.register_blueprint(auth_routes)
    auth_bp.register_blueprint(users_routes)
    auth_bp.register_blueprint(profile_routes)

    return auth_bp


auth_bp = create_auth_blueprint()
