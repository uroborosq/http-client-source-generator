from models import Manager, Request


class RequestService:
    def __init__(self):
        self.requests = []

    def add_request(self, client: str, manager, date_begin):
        request = Request(client=client,
                          manager=manager,
                          date_begin=date_begin)
        self.requests.append(request)
        return request

    def get_requests(self):
        return self.requests


class ManagersService:
    def __init__(self):
        self.managers = []

    def add_manager(self, name: str):
        manager = Manager(full_name=name)
        self.managers.append(manager)
        return manager

    def get_manger(self):
        return self.managers
