import { Route, Routes } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { ClaimSubmitPage } from '../pages/ClaimSubmitPage';
import { CustodyPage } from '../pages/CustodyPage';
import { LoginPage } from '../pages/LoginPage';
import { MatchesPage } from '../pages/MatchesPage';
import { MyClaimsPage } from '../pages/MyClaimsPage';
import { MyReportsPage } from '../pages/MyReportsPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { PublicBoardPage } from '../pages/PublicBoardPage';
import { RegisterPage } from '../pages/RegisterPage';
import { ReportFoundPage } from '../pages/ReportFoundPage';
import { ReportLostPage } from '../pages/ReportLostPage';
import { StaffDashboardPage } from '../pages/StaffDashboardPage';
import { ProtectedRoute } from './ProtectedRoute';

export function AppRouter() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<PublicBoardPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/report-lost"
          element={
            <ProtectedRoute>
              <ReportLostPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/report-found"
          element={
            <ProtectedRoute>
              <ReportFoundPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/my-reports"
          element={
            <ProtectedRoute>
              <MyReportsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/matches"
          element={
            <ProtectedRoute>
              <MatchesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/claim/:foundReportId"
          element={
            <ProtectedRoute>
              <ClaimSubmitPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/my-claims"
          element={
            <ProtectedRoute>
              <MyClaimsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff"
          element={
            <ProtectedRoute>
              <StaffDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/custody/:reportId"
          element={
            <ProtectedRoute>
              <CustodyPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
