from flask import Flask, jsonify
import os
from flask import Flask
from app.modules.auth import auth_bp
from app.modules.hris import couriers_bp
from dotenv import load_dotenv
from app.modules.auth.core.repositories import UserRepository

load_dotenv()


def create_app():
    app = Flask(__name__)

    # Configuration
    SECRET_KEY = os.getenv("SECRET_KEY")
    if not SECRET_KEY:
        raise ValueError("SECRET_KEY environment variable is required")

    app.config["SECRET_KEY"] = SECRET_KEY

    # Database configuration
    BASE_DIR = os.path.abspath(os.path.dirname(__file__))
    DB_PATH = os.path.join(BASE_DIR, "..", "dbs", "auth.db")
    DB_PATH = os.path.normpath(DB_PATH)
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    app.config["DATABASE"] = DB_PATH

    # Register blueprints
    app.register_blueprint(auth_bp)
    app.register_blueprint(couriers_bp)

    # Error handlers

    @app.errorhandler(404)
    def not_found(error):
        return jsonify({
            'error': 'Endpoint not found',
            'code': 'NOT_FOUND'
        }), 404

    @app.errorhandler(405)
    def method_not_allowed(error):
        return jsonify({
            'error': 'Method not allowed',
            'code': 'METHOD_NOT_ALLOWED'
        }), 405

    @app.errorhandler(500)
    def internal_error(error):
        return jsonify({
            'error': 'Internal server error',
            'code': 'INTERNAL_ERROR'
        }), 500

    # Health check endpoint
    @app.route('/health', methods=['GET'])
    def health_check():
        return jsonify({
            'status': 'healthy',
            'service': 'auth-service'
        }), 200
    return app


app = create_app()

if __name__ == "__main__":
    app.run(debug=True, host='0.0.0.0', port=5000)
