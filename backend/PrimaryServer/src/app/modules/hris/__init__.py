from flask import Blueprint
from .routes.couriers import couriers_routes


def create_couriers_blueprint():
    couriers_bp = Blueprint("couriers", __name__, url_prefix="/api")

    couriers_bp.register_blueprint(couriers_routes)

    return couriers_bp


couriers_bp = create_couriers_blueprint()
