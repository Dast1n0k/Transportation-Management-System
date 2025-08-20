from flask import Blueprint

authBlueprint = Blueprint("auth", __name__)

from .core.repositories import UserRepository