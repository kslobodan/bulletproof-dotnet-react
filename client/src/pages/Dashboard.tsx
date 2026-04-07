import { Navigation } from "../components/Navigation";
import { useAppSelector } from "../store/hooks";

export const Dashboard = () => {
  const { user, tenant } = useAppSelector((state) => state.auth);

  return (
    <>
      <Navigation />
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="bg-white shadow rounded-lg p-6">
            <h1 className="text-3xl font-bold text-gray-900 mb-6">
              Welcome to the Dashboard
            </h1>

            <div className="space-y-4">
              <div className="border-l-4 border-indigo-500 pl-4">
                <h2 className="text-xl font-semibold text-gray-800">
                  User Information
                </h2>
                <div className="mt-2 space-y-1 text-gray-600">
                  <p>
                    <span className="font-medium">Name:</span> {user?.firstName}{" "}
                    {user?.lastName}
                  </p>
                  <p>
                    <span className="font-medium">Email:</span> {user?.email}
                  </p>
                  <p>
                    <span className="font-medium">ID:</span> {user?.id}
                  </p>
                  {user?.roles && user.roles.length > 0 && (
                    <p>
                      <span className="font-medium">Roles:</span>{" "}
                      {user.roles.map((role) => (
                        <span
                          key={role}
                          className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800 ml-1"
                        >
                          {role}
                        </span>
                      ))}
                    </p>
                  )}
                </div>
              </div>

              <div className="border-l-4 border-green-500 pl-4">
                <h2 className="text-xl font-semibold text-gray-800">
                  Tenant Information
                </h2>
                <div className="mt-2 space-y-1 text-gray-600">
                  <p>
                    <span className="font-medium">Tenant Name:</span>{" "}
                    {tenant?.name}
                  </p>
                  <p>
                    <span className="font-medium">Plan:</span> {tenant?.plan}
                  </p>
                  <p>
                    <span className="font-medium">Tenant ID:</span> {tenant?.id}
                  </p>
                </div>
              </div>

              <div className="mt-8 p-4 bg-blue-50 rounded-md">
                <p className="text-sm text-blue-800">
                  🎉 Authentication is working! You are now logged in and can
                  access protected routes.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};
